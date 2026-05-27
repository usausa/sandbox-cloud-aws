namespace Tenant.IaC.Constants;

internal static class ApiGatewayConstants
{
    public const string ApiConstructId = "TenantHttpApi";
    public const string AuthorizerConstructId = "TenantJwtAuthorizer";
    public const string IntegrationConstructId = "TenantAlbIntegration";
    public const string RouteConstructId = "TenantProxyRoute";
    public const string StageConstructId = "TenantDefaultStage";
    public const string ApiEndpointOutputId = "ApiEndpoint";
    public const string ApiName = "tenant-api";
    public const string AuthorizerName = "tenant-jwt-authorizer";
    public const string ProxyRoute = "/{proxy+}";
    public const string ProxyTargetPath = "{proxy}";
    public const string AllowAnyOrigin = "*";
    public const string AuthorizationHeader = "Authorization";
    public const string ContentTypeHeader = "Content-Type";
    public const string HttpProtocolType = "HTTP";
    public const string AnyMethod = "ANY";
    public const string JwtAuthorizerType = "JWT";
    public const string JwtAuthorizationType = "JWT";
    public const string IdentitySourceAuthorizationHeader = "$request.header.Authorization";
    public const string HttpProxyIntegrationType = "HTTP_PROXY";
    public const string PayloadFormatVersion = "1.0";
    public const string DefaultStageName = "$default";
}
