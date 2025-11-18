using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ServiceDiscovery;

using Constructs;

namespace TenantApi.IaC.Constructs;

internal sealed class ApiConstructProps
{
    public required Vpc Vpc { get; init; }

    public required UserPool UserPool { get; init; }

    public required UserPoolClient UserPoolClient { get; init; }

    public required Amazon.CDK.AWS.ServiceDiscovery.IService CloudMapService { get; init; }

    public required SecurityGroup VpcLinkSecurityGroup { get; init; }
}

/// <summary>
/// API Gateway HTTP API with JWT authorizer (Cognito access tokens) and a
/// VPC Link integration into the Cloud Map / ECS service. Claim-to-header
/// mapping uses <c>overwrite:header</c> via the L1 <see cref="CfnIntegration"/>
/// to prevent client header spoofing.
/// </summary>
internal sealed class ApiConstruct : Construct
{
    public string HttpApiUrl { get; }

    public ApiConstruct(Construct scope, string id, ApiConstructProps props)
        : base(scope, id)
    {
        var api = new CfnApi(this, "HttpApi", new CfnApiProps
        {
            Name = Constants.HttpApiName,
            ProtocolType = "HTTP",
        });

        var stage = new CfnStage(this, "DefaultStage", new CfnStageProps
        {
            ApiId = api.Ref,
            StageName = "$default",
            AutoDeploy = true,
        });

        var vpcLink = new CfnVpcLink(this, "VpcLink", new CfnVpcLinkProps
        {
            Name = $"{Constants.AppName}-vpc-link",
            SubnetIds = props.Vpc.SelectSubnets(new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED }).SubnetIds,
            SecurityGroupIds = [props.VpcLinkSecurityGroup.SecurityGroupId],
        });

        var region = Stack.Of(this).Region;
        var issuer = $"https://cognito-idp.{region}.amazonaws.com/{props.UserPool.UserPoolId}";

        var authorizer = new CfnAuthorizer(this, "JwtAuthorizer", new CfnAuthorizerProps
        {
            ApiId = api.Ref,
            Name = "CognitoJwt",
            AuthorizerType = "JWT",
            IdentitySource = ["$request.header.Authorization"],
            JwtConfiguration = new CfnAuthorizer.JWTConfigurationProperty
            {
                Issuer = issuer,
                Audience = [props.UserPoolClient.UserPoolClientId],
            },
        });

        var integration = new CfnIntegration(this, "EcsIntegration", new CfnIntegrationProps
        {
            ApiId = api.Ref,
            IntegrationType = "HTTP_PROXY",
            IntegrationMethod = "ANY",
            IntegrationUri = props.CloudMapService.ServiceArn,
            ConnectionType = "VPC_LINK",
            ConnectionId = vpcLink.Ref,
            PayloadFormatVersion = "1.0",
            RequestParameters = new Dictionary<string, string>
            {
                // Overwrite incoming X-* headers with values from JWT claims (prevents client spoofing).
                [$"overwrite:header.{Constants.TenantHeaderName}"] = $"$context.authorizer.claims.{Constants.TenantClaimKey}",
                [$"overwrite:header.{Constants.UserSubHeaderName}"] = $"$context.authorizer.claims.{Constants.UserSubClaimKey}",
            },
        });

        // Authenticated route: GET /tenant
        _ = new Amazon.CDK.AWS.Apigatewayv2.CfnRoute(this, "TenantRoute", new Amazon.CDK.AWS.Apigatewayv2.CfnRouteProps
        {
            ApiId = api.Ref,
            RouteKey = $"GET {Constants.ApiRoutePath}",
            AuthorizationType = "JWT",
            AuthorizerId = authorizer.Ref,
            Target = $"integrations/{integration.Ref}",
        });

        // Health check route: no auth required (for ECS health verification via VPC Link)
        _ = new Amazon.CDK.AWS.Apigatewayv2.CfnRoute(this, "HealthRoute", new Amazon.CDK.AWS.Apigatewayv2.CfnRouteProps
        {
            ApiId = api.Ref,
            RouteKey = "GET /health",
            AuthorizationType = "NONE",
            Target = $"integrations/{integration.Ref}",
        });

        _ = stage; // referenced for dependency

        HttpApiUrl = $"https://{api.Ref}.execute-api.{region}.amazonaws.com";
    }
}
