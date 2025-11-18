using System.Collections.Generic;

using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.WAFv2;

using Constructs;

using Serverless.IaC.Configuration;

namespace Serverless.IaC.Stacks;

/// <summary>
/// CloudFront 用エッジリソース(us-east-1 固定)。
/// ACM 証明書と WAF WebACL を配置する。
/// </summary>
internal sealed class EdgeStack : Stack
{
    public EdgeStack(Construct scope, string id, IStackProps props)
        : base(scope, id, props)
    {
        var hostedZone = HostedZone.FromLookup(this, "HostedZone", new HostedZoneProviderProps
        {
            DomainName = ProjectConstants.HostedZoneName,
        });

        Certificate = new Certificate(this, "Certificate", new CertificateProps
        {
            DomainName = ProjectConstants.ServiceDomainName,
            Validation = CertificateValidation.FromDns(hostedZone),
        });

        WebAcl = new CfnWebACL(this, "WebAcl", new CfnWebACLProps
        {
            Name = $"{ProjectConstants.ResourceSuffix}-acl",
            Scope = "CLOUDFRONT",
            DefaultAction = new CfnWebACL.DefaultActionProperty { Allow = new CfnWebACL.AllowActionProperty() },
            VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
            {
                CloudWatchMetricsEnabled = true,
                MetricName = $"{ProjectConstants.ResourceSuffix}-acl",
                SampledRequestsEnabled = true,
            },
            Rules = new object[]
            {
                BuildManagedRule("AWSManagedRulesAmazonIpReputationList", priority: 0),
                BuildManagedRule("AWSManagedRulesAnonymousIpList", priority: 1),
                BuildManagedRule("AWSManagedRulesKnownBadInputsRuleSet", priority: 2),
                BuildManagedRule("AWSManagedRulesCommonRuleSet", priority: 3),
                BuildManagedRule("AWSManagedRulesSQLiRuleSet", priority: 4),
                BuildRateLimitRule(priority: 5),
            },
        });
    }

    public ICertificate Certificate { get; }

    public CfnWebACL WebAcl { get; }

    private static CfnWebACL.RuleProperty BuildManagedRule(string name, int priority) => new()
    {
        Name = name,
        Priority = priority,
        OverrideAction = new CfnWebACL.OverrideActionProperty { None = new Dictionary<string, object>() },
        Statement = new CfnWebACL.StatementProperty
        {
            ManagedRuleGroupStatement = new CfnWebACL.ManagedRuleGroupStatementProperty
            {
                VendorName = "AWS",
                Name = name,
            },
        },
        VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
        {
            CloudWatchMetricsEnabled = true,
            MetricName = name,
            SampledRequestsEnabled = true,
        },
    };

    private static CfnWebACL.RuleProperty BuildRateLimitRule(int priority) => new()
    {
        Name = "RateLimit",
        Priority = priority,
        Action = new CfnWebACL.RuleActionProperty { Block = new CfnWebACL.BlockActionProperty() },
        Statement = new CfnWebACL.StatementProperty
        {
            RateBasedStatement = new CfnWebACL.RateBasedStatementProperty
            {
                Limit = ProjectConstants.WafRateLimitPer5Minutes,
                AggregateKeyType = "IP",
            },
        },
        VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
        {
            CloudWatchMetricsEnabled = true,
            MetricName = "RateLimit",
            SampledRequestsEnabled = true,
        },
    };
}
