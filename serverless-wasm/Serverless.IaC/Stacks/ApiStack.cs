using System.Collections.Generic;

using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;

using Constructs;

using ILambdaFunction = Amazon.CDK.AWS.Lambda.IFunction;

using Serverless.IaC.Configuration;

namespace Serverless.IaC.Stacks;

internal sealed class ApiStackProps : StackProps
{
    public required IVpc Vpc { get; init; }

    public required ISecurityGroup LambdaSecurityGroup { get; init; }

    public required string ParameterPrefix { get; init; }
}

/// <summary>
/// API Gateway (HTTP API) と Lambda 群。
/// JWT Authorizer / 直叩き抑止用カスタムヘッダー / VPC 接続を構成する。
/// 個別関数のソース実装は別リポジトリ/プロジェクトで管理する想定。
/// </summary>
internal sealed class ApiStack : Stack
{
    public ApiStack(Construct scope, string id, ApiStackProps props)
        : base(scope, id, props)
    {
        // Lambda 共通実行ロール
        var executionRole = new Role(this, "LambdaExecutionRole", new RoleProps
        {
            RoleName = $"{ProjectConstants.ResourceSuffix}-lambda-role",
            AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
            ManagedPolicies =
            [
                ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"),
                ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaVPCAccessExecutionRole"),
            ],
        });

        executionRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["ssm:GetParameter", "ssm:GetParameters", "ssm:GetParametersByPath"],
            Resources = [$"arn:aws:ssm:{Region}:{Account}:parameter{props.ParameterPrefix}/*"],
        }));

        // サンプル関数(VPC 外)
        var publicFn = CreateFunction(
            id: "HealthFunction",
            functionName: $"{ProjectConstants.ResourceSuffix}-health",
            handler: "Serverless.Functions::Serverless.Functions.Health::Handler",
            role: executionRole,
            useVpc: false,
            props: props);

        // サンプル関数(VPC 内: 外部接続が固定 IP 必要)
        var privateFn = CreateFunction(
            id: "ExternalIntegrationFunction",
            functionName: $"{ProjectConstants.ResourceSuffix}-external",
            handler: "Serverless.Functions::Serverless.Functions.External::Handler",
            role: executionRole,
            useVpc: true,
            props: props);

        Functions = [publicFn, privateFn];

        // CloudFront → API Gateway の直叩き抑止用シークレットヘッダー値
        OriginVerifyHeaderValue = $"{ProjectConstants.ResourceSuffix}-{Names.UniqueId(this)[..8]}";

        // HTTP API (L2)
        HttpApi = new HttpApi(this, "HttpApi", new HttpApiProps
        {
            ApiName = $"{ProjectConstants.ResourceSuffix}-api",
            CreateDefaultStage = true,
        });

        // JWT Authorizer
        _ = new HttpAuthorizer(this, "JwtAuthorizer", new HttpAuthorizerProps
        {
            HttpApi = HttpApi,
            AuthorizerName = "JwtAuthorizer",
            Type = HttpAuthorizerType.JWT,
            IdentitySource = ["$request.header.Authorization"],
            JwtIssuer = ProjectConstants.JwtIssuer,
            JwtAudience = [ProjectConstants.JwtAudience],
        });

        _ = new CfnOutput(this, "HttpApiEndpoint", new CfnOutputProps
        {
            Value = HttpApi.Url ?? $"https://{HttpApi.HttpApiId}.execute-api.{Region}.amazonaws.com/",
            ExportName = $"{ProjectConstants.ResourceSuffix}-api-endpoint",
        });
    }

    public HttpApi HttpApi { get; }

    public IReadOnlyList<ILambdaFunction> Functions { get; }

    public string OriginVerifyHeaderValue { get; }

    private Function CreateFunction(
        string id,
        string functionName,
        string handler,
        IRole role,
        bool useVpc,
        ApiStackProps props)
    {
        var functionProps = new FunctionProps
        {
            FunctionName = functionName,
            Runtime = ProjectConstants.LambdaRuntime,
            Architecture = ProjectConstants.LambdaArchitecture,
            Handler = handler,
            // 実コードは別途配置。CDK 単体合成可能なよう inline ダミーにしておく。
            Code = Code.FromInline("exports.handler = async () => ({ statusCode: 200, body: 'ok' });"),
            MemorySize = ProjectConstants.LambdaMemorySizeMb,
            Timeout = Duration.Seconds(ProjectConstants.LambdaTimeoutSeconds),
            Role = role,
            LogRetention = ProjectConstants.LambdaLogRetention,
            ReservedConcurrentExecutions = ProjectConstants.LambdaReservedConcurrency,
            Environment = new Dictionary<string, string>
            {
                ["SSM_PARAMETER_PREFIX"] = props.ParameterPrefix,
                ["ENVIRONMENT"] = ProjectConstants.EnvironmentName,
            },
        };

        if (useVpc)
        {
            functionProps.Vpc = props.Vpc;
            functionProps.VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS };
            functionProps.SecurityGroups = [props.LambdaSecurityGroup];
        }

        return new Function(this, id, functionProps);
    }
}
