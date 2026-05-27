// <copyright file="WafStack.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Stacks;

using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.WAFv2;
using Constructs;
using Serverless.IaC.Constants;

internal sealed class WafStack : Stack
{
    public WafStack(Construct scope, string id, IBucket auditBucket, IStackProps? props = null)
        : base(scope, id, props)
    {
        var webAcl = new CfnWebACL(this, "WebAcl", new CfnWebACLProps
        {
            Name = $"{AppConstants.StackPrefix}-WebAcl",
            Scope = WafConstants.Scope,
            DefaultAction = new CfnWebACL.DefaultActionProperty
            {
                Allow = new CfnWebACL.AllowActionProperty(),
            },
            VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
            {
                CloudWatchMetricsEnabled = true,
                MetricName = $"{AppConstants.StackPrefix}-WebAcl",
                SampledRequestsEnabled = true,
            },
            Rules = new object[]
            {
                CreateManagedRuleGroup("AWSManagedRulesAmazonIpReputationList", WafConstants.IpReputationPriority),
                CreateManagedRuleGroup("AWSManagedRulesAnonymousIpList", WafConstants.AnonymousIpPriority),
                CreateManagedRuleGroup("AWSManagedRulesKnownBadInputsRuleSet", WafConstants.KnownBadInputsPriority),
                CreateManagedRuleGroup("AWSManagedRulesCommonRuleSet", WafConstants.CommonRuleSetPriority),
                CreateManagedRuleGroup("AWSManagedRulesSQLiRuleSet", WafConstants.SqliPriority),
                new CfnWebACL.RuleProperty
                {
                    Name = "RateLimit",
                    Priority = WafConstants.RateLimitPriority,
                    Action = new CfnWebACL.RuleActionProperty
                    {
                        Block = new CfnWebACL.BlockActionProperty(),
                    },
                    VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
                    {
                        CloudWatchMetricsEnabled = true,
                        MetricName = "RateLimit",
                        SampledRequestsEnabled = true,
                    },
                    Statement = new CfnWebACL.StatementProperty
                    {
                        RateBasedStatement = new CfnWebACL.RateBasedStatementProperty
                        {
                            Limit = WafConstants.RateLimitPer5Min,
                            AggregateKeyType = "IP",
                        },
                    },
                },
            },
        });

        _ = new CfnLoggingConfiguration(this, "WafLogging", new CfnLoggingConfigurationProps
        {
            ResourceArn = webAcl.AttrArn,
            LogDestinationConfigs = [auditBucket.BucketArn],
        });

        this.WebAclArn = webAcl.AttrArn;

        _ = new CfnOutput(this, "WebAclArn", new CfnOutputProps
        {
            Description = "Web ACL ARN for the CloudFront distribution",
            Value = this.WebAclArn,
        });
    }

    public string WebAclArn { get; }

    private static CfnWebACL.RuleProperty CreateManagedRuleGroup(string name, int priority)
    {
        return new CfnWebACL.RuleProperty
        {
            Name = name,
            Priority = priority,
            OverrideAction = new CfnWebACL.OverrideActionProperty
            {
                None = new Dictionary<string, object>(),
            },
            VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
            {
                CloudWatchMetricsEnabled = true,
                MetricName = name,
                SampledRequestsEnabled = true,
            },
            Statement = new CfnWebACL.StatementProperty
            {
                ManagedRuleGroupStatement = new CfnWebACL.ManagedRuleGroupStatementProperty
                {
                    Name = name,
                    VendorName = "AWS",
                },
            },
        };
    }
}
