namespace Tenant.IaC.Constants;

internal static class CognitoConstants
{
    public const string UserPoolConstructId = "TenantUserPool";
    public const string UserPoolClientConstructId = "TenantUserPoolClient";
    public const string PreTokenLambdaConstructId = "PreTokenGenerationLambda";
    public const string PreTokenPermissionId = "AllowCognitoInvokePreTokenLambda";
    public const string UserPoolIdOutputId = "UserPoolId";
    public const string UserPoolClientIdOutputId = "UserPoolClientId";
    public const string UserPoolIssuerUrlOutputId = "UserPoolIssuerUrl";
    public const string UserPoolName = "tenant-user-pool";
    public const string AppClientName = "tenant-app-client";
    public const string TenantIdAttributeName = "tenantId";
    public const string TenantIdClaimName = "custom:tenantId";
    public const int AccessTokenValidityMinutes = 60;
    public const int IdTokenValidityMinutes = 60;
    public const int RefreshTokenValidityDays = 30;
    public const int MinimumPasswordLength = 8;
    public const string PreTokenLambdaName = "tenant-pre-token-generation";
    public const string CognitoPrincipal = "cognito-idp.amazonaws.com";
    public const string LambdaInvokeAction = "lambda:InvokeFunction";
    public const string PreTokenLambdaVersion = "V2_0";

    // TODO: 要定義 - 本番では MFA REQUIRED を検討すること
    public const string MfaMode = "OPTIONAL";
}
