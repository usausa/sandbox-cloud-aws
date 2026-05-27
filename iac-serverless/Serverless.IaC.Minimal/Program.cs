// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
using Amazon.CDK;
using Serverless.IaC.Minimal.Constants;
using Serverless.IaC.Minimal.Stacks;

var app = new App();
var account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT");
var primaryEnvironment = new Amazon.CDK.Environment
{
    Account = account,
    Region = AppConstants.PrimaryRegion,
};
var usEast1Environment = new Amazon.CDK.Environment
{
    Account = account,
    Region = AppConstants.CloudFrontCertificateRegion,
};

Tags.Of(app).Add(nameof(TaggingConstants.Project), TaggingConstants.Project);
Tags.Of(app).Add(nameof(TaggingConstants.Environment), TaggingConstants.Environment);
Tags.Of(app).Add(nameof(TaggingConstants.ManagedBy), TaggingConstants.ManagedBy);

var dns = new DnsStack(app, $"{AppConstants.StackPrefix}-Dns", new StackProps { Env = primaryEnvironment });
var certificate = new CertificateStack(
    app,
    $"{AppConstants.StackPrefix}-Certificate",
    dns.HostedZone,
    new StackProps
    {
        Env = usEast1Environment,
        CrossRegionReferences = true,
    });
var data = new DataStack(app, $"{AppConstants.StackPrefix}-Data", new StackProps { Env = primaryEnvironment });
var storage = new StorageStack(app, $"{AppConstants.StackPrefix}-Storage", new StackProps { Env = primaryEnvironment });
var waf = new WafStack(
    app,
    $"{AppConstants.StackPrefix}-Waf",
    new StackProps
    {
        Env = usEast1Environment,
        CrossRegionReferences = true,
    });
var compute = new ComputeStack(
    app,
    $"{AppConstants.StackPrefix}-Compute",
    data.SampleTable,
    new StackProps { Env = primaryEnvironment });
var api = new ApiStack(
    app,
    $"{AppConstants.StackPrefix}-Api",
    compute.SampleFunction,
    new StackProps { Env = primaryEnvironment });
var edge = new EdgeStack(
    app,
    $"{AppConstants.StackPrefix}-Edge",
    storage.SpaBucket,
    api.ApiDomainName,
    certificate.Certificate,
    waf.WebAclArn,
    new StackProps
    {
        Env = primaryEnvironment,
        CrossRegionReferences = true,
    });

dns.AddAliasRecord(edge.Distribution);

_ = new ObservabilityStack(
    app,
    $"{AppConstants.StackPrefix}-Observability",
    compute.SampleFunction,
    api.HttpApi,
    edge.Distribution,
    data.SampleTable,
    new StackProps { Env = primaryEnvironment });

app.Synth();
