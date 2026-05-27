namespace Tenant.IaC.Stacks;

using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Constructs;
using Tenant.IaC.Constants;

internal sealed class ApiGatewayStack : Stack
{
    public ApiGatewayStack(Construct scope, string id, StackProps props, CognitoStack cognitoStack, EcsStack ecsStack)
        : base(scope, id, props)
    {
        var httpApi = new CfnApi(this, ApiGatewayConstants.ApiConstructId, new CfnApiProps
        {
            Name = ApiGatewayConstants.ApiName,
            ProtocolType = ApiGatewayConstants.HttpProtocolType,
            CorsConfiguration = new CfnApi.CorsProperty
            {
                AllowHeaders = [ApiGatewayConstants.AuthorizationHeader, ApiGatewayConstants.ContentTypeHeader],
                AllowMethods = [ApiGatewayConstants.AnyMethod],
                AllowOrigins = [ApiGatewayConstants.AllowAnyOrigin],
            },
        });

        var authorizer = new CfnAuthorizer(this, ApiGatewayConstants.AuthorizerConstructId, new CfnAuthorizerProps
        {
            ApiId = httpApi.Ref,
            AuthorizerType = ApiGatewayConstants.JwtAuthorizerType,
            IdentitySource = [ApiGatewayConstants.IdentitySourceAuthorizationHeader],
            JwtConfiguration = new CfnAuthorizer.JWTConfigurationProperty
            {
                Audience = [cognitoStack.UserPoolClient.UserPoolClientId],
                Issuer = cognitoStack.UserPoolIssuerUrl,
            },
            Name = ApiGatewayConstants.AuthorizerName,
        });

        var integration = new CfnIntegration(this, ApiGatewayConstants.IntegrationConstructId, new CfnIntegrationProps
        {
            ApiId = httpApi.Ref,
            IntegrationMethod = ApiGatewayConstants.AnyMethod,
            IntegrationType = ApiGatewayConstants.HttpProxyIntegrationType,
            IntegrationUri = $"http://{ecsStack.AlbDnsName}/{ApiGatewayConstants.ProxyTargetPath}",
            PayloadFormatVersion = ApiGatewayConstants.PayloadFormatVersion,
        });

        _ = new CfnRoute(this, ApiGatewayConstants.RouteConstructId, new CfnRouteProps
        {
            ApiId = httpApi.Ref,
            AuthorizerId = authorizer.Ref,
            AuthorizationType = ApiGatewayConstants.JwtAuthorizationType,
            RouteKey = $"{ApiGatewayConstants.AnyMethod} {ApiGatewayConstants.ProxyRoute}",
            Target = $"integrations/{integration.Ref}",
        });

        _ = new CfnStage(this, ApiGatewayConstants.StageConstructId, new CfnStageProps
        {
            ApiId = httpApi.Ref,
            AutoDeploy = true,
            StageName = ApiGatewayConstants.DefaultStageName,
        });

        _ = new CfnOutput(this, ApiGatewayConstants.ApiEndpointOutputId, new CfnOutputProps
        {
            Value = httpApi.AttrApiEndpoint,
        });
    }
}
