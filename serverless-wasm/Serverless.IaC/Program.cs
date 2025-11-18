using Amazon.CDK;

using Serverless.IaC.Configuration;
using Serverless.IaC.Stacks;

namespace Serverless.IaC;

/// <summary>
/// CDK アプリケーションのエントリポイント。
/// スタック構成は責務(ネットワーク / データ / API / エッジ / 監視 / 監査)で分割する。
/// </summary>
internal static class Program
{
    public static void Main()
    {
        var app = new App();

        var primaryEnv = ProjectConstants.GetPrimaryEnvironment();
        var edgeEnv = ProjectConstants.GetEdgeEnvironment();
        var prefix = ProjectConstants.ResourceSuffix;

        // 1) ネットワーク (VPC / NAT / Endpoint)
        var networkStack = new NetworkStack(app, $"{prefix}-network", new StackProps
        {
            Env = primaryEnv,
            Description = "VPC / Subnets / NAT / VPC Endpoints",
        });

        // 2) データ層 (DynamoDB / SSM Parameter Store)
        var dataStack = new DataStack(app, $"{prefix}-data", new StackProps
        {
            Env = primaryEnv,
            Description = "DynamoDB tables and SSM Parameters",
        });

        // 3) ストレージ (S3: SPA / 監査 / SAM artifacts)
        var storageStack = new StorageStack(app, $"{prefix}-storage", new StackProps
        {
            Env = primaryEnv,
            Description = "S3 buckets (SPA / Audit / SAM artifacts)",
        });

        // 4) API (Lambda / API Gateway HTTP API)
        var apiStack = new ApiStack(app, $"{prefix}-api", new ApiStackProps
        {
            Env = primaryEnv,
            Description = "Lambda functions and API Gateway HTTP API",
            Vpc = networkStack.Vpc,
            LambdaSecurityGroup = networkStack.LambdaSecurityGroup,
            ParameterPrefix = dataStack.ParameterPrefix,
        });

        // 5) エッジ (us-east-1: ACM / WAF)
        var edgeStack = new EdgeStack(app, $"{prefix}-edge", new StackProps
        {
            Env = edgeEnv,
            Description = "ACM certificate and WAF WebACL for CloudFront (us-east-1)",
            CrossRegionReferences = true,
        });

        // 6) CDN (CloudFront / Route 53)
        var cdnStack = new CdnStack(app, $"{prefix}-cdn", new CdnStackProps
        {
            Env = primaryEnv,
            Description = "CloudFront distribution and Route 53 alias",
            CrossRegionReferences = true,
            SpaBucket = storageStack.SpaBucket,
            HttpApi = apiStack.HttpApi,
            Certificate = edgeStack.Certificate,
            WebAcl = edgeStack.WebAcl,
            OriginVerifyHeaderValue = apiStack.OriginVerifyHeaderValue,
        });

        // 7) 監査 (CloudTrail)
        _ = new AuditStack(app, $"{prefix}-audit", new AuditStackProps
        {
            Env = primaryEnv,
            Description = "CloudTrail (multi-region) into audit S3 bucket",
            AuditLogBucket = storageStack.AuditLogBucket,
        });

        // 8) 監視 (CloudWatch Alarms / Dashboard / SNS)
        _ = new MonitoringStack(app, $"{prefix}-monitoring", new MonitoringStackProps
        {
            Env = primaryEnv,
            Description = "CloudWatch Alarms, Dashboard and SNS topics",
            ApiFunctions = apiStack.Functions,
            HttpApi = apiStack.HttpApi,
            Tables = dataStack.Tables,
            Distribution = cdnStack.Distribution,
        });

        Tags.Of(app).Add("Project", ProjectConstants.ProjectName);
        Tags.Of(app).Add("Environment", ProjectConstants.EnvironmentName);
        Tags.Of(app).Add("ManagedBy", "CDK");

        app.Synth();
    }
}
