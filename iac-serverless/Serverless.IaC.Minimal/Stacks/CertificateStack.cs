// <copyright file="CertificateStack.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Minimal.Stacks;

using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.Route53;
using Constructs;
using Serverless.IaC.Minimal.Constants;

internal sealed class CertificateStack : Stack
{
    public CertificateStack(Construct scope, string id, IHostedZone hostedZone, IStackProps? props = null)

        : base(scope, id, props)
    {
        this.Certificate = new Certificate(this, nameof(this.Certificate), new CertificateProps
        {
            DomainName = DomainConstants.SiteDomain,

            SubjectAlternativeNames = [$"*.{DomainConstants.RootDomain}"],

            Validation = CertificateValidation.FromDns(hostedZone),
        });
    }

    public ICertificate Certificate { get; }
}
