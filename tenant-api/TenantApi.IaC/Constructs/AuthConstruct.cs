using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.Lambda;

using Constructs;

namespace TenantApi.IaC.Constructs;

/// <summary>
/// Cognito User Pool, App Client and Pre Token Generation Lambda (V2_0).
/// The Lambda injects the user's <c>custom:tenant_id</c> attribute into the
/// access token so API Gateway can forward it as the <c>X-Tenant-Id</c>
/// header.
/// </summary>
internal sealed class AuthConstruct : Construct
{
    public UserPool UserPool { get; }

    public UserPoolClient UserPoolClient { get; }

    public Function PreTokenLambda { get; }

    public AuthConstruct(Construct scope, string id)
        : base(scope, id)
    {
        PreTokenLambda = new Function(this, "PreTokenLambda", new FunctionProps
        {
            Runtime = Runtime.NODEJS_22_X,
            Handler = "index.handler",
            Code = Code.FromAsset(Constants.PreTokenLambdaPath),
            Timeout = Duration.Seconds(5),
            MemorySize = 128,
        });

        UserPool = new UserPool(this, "UserPool", new UserPoolProps
        {
            UserPoolName = Constants.UserPoolName,
            SelfSignUpEnabled = false,
            SignInAliases = new SignInAliases { Email = true },
            CustomAttributes = new Dictionary<string, ICustomAttribute>
            {
                [Constants.TenantIdAttributeName] = new StringAttribute(new StringAttributeProps { Mutable = true }),
            },
            FeaturePlan = FeaturePlan.PLUS,
            // V2_0 trigger is set via L1 escape hatch below.
            LambdaTriggers = new UserPoolTriggers
            {
                PreTokenGeneration = PreTokenLambda,
            },
            RemovalPolicy = RemovalPolicy.DESTROY,
        });

        // Upgrade the trigger to V2_0 via the CloudFormation L1 escape hatch.
        // CDK L2 (2.x) does not expose PreTokenGenerationConfig directly.
        var cfnUserPool = (CfnUserPool)UserPool.Node.DefaultChild!;
        cfnUserPool.LambdaConfig = new CfnUserPool.LambdaConfigProperty
        {
            PreTokenGenerationConfig = new CfnUserPool.PreTokenGenerationConfigProperty
            {
                LambdaArn = PreTokenLambda.FunctionArn,
                LambdaVersion = "V2_0",
            },
        };

        // Allow Cognito to invoke the Lambda.
        PreTokenLambda.AddPermission("CognitoInvoke", new Permission
        {
            Principal = new Amazon.CDK.AWS.IAM.ServicePrincipal("cognito-idp.amazonaws.com"),
            SourceArn = UserPool.UserPoolArn,
        });

        UserPoolClient = UserPool.AddClient("AppClient", new UserPoolClientOptions
        {
            UserPoolClientName = Constants.UserPoolClientName,
            GenerateSecret = false,
            AuthFlows = new AuthFlow
            {
                UserSrp = true,
            },
            OAuth = new OAuthSettings
            {
                Flows = new OAuthFlows { AuthorizationCodeGrant = true },
                Scopes = [OAuthScope.OPENID, OAuthScope.EMAIL, OAuthScope.PROFILE],
            },
            AccessTokenValidity = Duration.Hours(24),
            IdTokenValidity = Duration.Hours(24),
            RefreshTokenValidity = Duration.Days(30),
        });
    }
}
