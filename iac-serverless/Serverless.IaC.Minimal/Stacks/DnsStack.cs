// <copyright file="DnsStack.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Minimal.Stacks;

using Amazon.CDK;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.Route53.Targets;
using Constructs;
using Serverless.IaC.Minimal.Constants;

internal sealed class DnsStack : Stack
{
    public DnsStack(Construct scope, string id, IStackProps? props = null)

        : base(scope, id, props)
    {
        this.HostedZone = DomainConstants.UseExistingHostedZone

            ? Amazon.CDK.AWS.Route53.HostedZone.FromLookup(this, nameof(this.HostedZone), new HostedZoneProviderProps
            {
                DomainName = DomainConstants.RootDomain,
            })

            : new PublicHostedZone(this, nameof(this.HostedZone), new PublicHostedZoneProps
            {
                ZoneName = DomainConstants.RootDomain,
            });
    }

    public IHostedZone HostedZone { get; }

    public void AddAliasRecord(IDistribution distribution)
    {
        _ = new ARecord(this, "SiteAliasRecord", new ARecordProps
        {
            Zone = this.HostedZone,

            RecordName = DomainConstants.SiteDomain,

            Target = RecordTarget.FromAlias(new CloudFrontTarget(distribution)),
        });
    }
}
