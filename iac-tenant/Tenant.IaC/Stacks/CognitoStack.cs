namespace Tenant.IaC.Stacks;

using System;
using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using Tenant.IaC.Constants;

internal sealed class CognitoStack : Stack
{
    public CognitoStack(Construct scope, string id, StackProps props)
        : base(scope, id, props)
    {
        var preTokenLambda = new Function(this, CognitoConstants.PreTokenLambdaConstructId, new FunctionProps
        {
            FunctionName = CognitoConstants.PreTokenLambdaName,
            Runtime = Runtime.NODEJS_22_X,
            Handler = "index.handler",
            Code = Code.FromAsset("../lambda/pre-token-generation"),
        });

        var userPool = new UserPool(this, CognitoConstants.UserPoolConstructId, new UserPoolProps
        {
            UserPoolName = CognitoConstants.UserPoolName,
            SignInAliases = new SignInAliases { Email = true },
            PasswordPolicy = new PasswordPolicy
            {
                MinLength = CognitoConstants.MinimumPasswordLength,
                RequireDigits = true,
                RequireLowercase = true,
                RequireUppercase = true,
            },
            Mfa = ResolveMfaMode(),
            AccountRecovery = AccountRecovery.EMAIL_ONLY,
            AutoVerify = new AutoVerifiedAttrs { Email = true },
            SelfSignUpEnabled = false,
            CustomAttributes = new Dictionary<string, ICustomAttribute>
            {
                [CognitoConstants.TenantIdAttributeName] = new StringAttribute(new StringAttributeProps
                {
                    Mutable = true,
                }),
            },
        });

        if (userPool.Node.DefaultChild is not CfnUserPool cfnUserPool)
        {
            throw new InvalidOperationException("Expected user pool default child to be a CfnUserPool.");
        }

        cfnUserPool.LambdaConfig = new CfnUserPool.LambdaConfigProperty
        {
            PreTokenGenerationConfig = new CfnUserPool.PreTokenGenerationConfigProperty
            {
                LambdaArn = preTokenLambda.FunctionArn,
                LambdaVersion = CognitoConstants.PreTokenLambdaVersion,
            },
        };

        preTokenLambda.AddPermission(CognitoConstants.PreTokenPermissionId, new Permission
        {
            Action = CognitoConstants.LambdaInvokeAction,
            Principal = new ServicePrincipal(CognitoConstants.CognitoPrincipal),
            SourceArn = userPool.UserPoolArn,
        });

        var userPoolClient = userPool.AddClient(CognitoConstants.UserPoolClientConstructId, new UserPoolClientOptions
        {
            UserPoolClientName = CognitoConstants.AppClientName,
            GenerateSecret = false,
            AuthFlows = new AuthFlow
            {
                UserSrp = true,
                UserPassword = true,
            },
            AccessTokenValidity = Duration.Minutes(CognitoConstants.AccessTokenValidityMinutes),
            IdTokenValidity = Duration.Minutes(CognitoConstants.IdTokenValidityMinutes),
            RefreshTokenValidity = Duration.Days(CognitoConstants.RefreshTokenValidityDays),
        });

        var issuerUrl = $"https://cognito-idp.{Region}.amazonaws.com/{userPool.UserPoolId}";

        UserPool = userPool;
        UserPoolClient = userPoolClient;
        UserPoolIssuerUrl = issuerUrl;

        _ = new CfnOutput(this, CognitoConstants.UserPoolIdOutputId, new CfnOutputProps
        {
            Value = UserPool.UserPoolId,
        });
        _ = new CfnOutput(this, CognitoConstants.UserPoolClientIdOutputId, new CfnOutputProps
        {
            Value = UserPoolClient.UserPoolClientId,
        });
        _ = new CfnOutput(this, CognitoConstants.UserPoolIssuerUrlOutputId, new CfnOutputProps
        {
            Value = UserPoolIssuerUrl,
        });
    }

    public IUserPool UserPool { get; }

    public IUserPoolClient UserPoolClient { get; }

    public string UserPoolIssuerUrl { get; }

    private static Mfa ResolveMfaMode() => CognitoConstants.MfaMode switch
    {
        "OFF" => Mfa.OFF,
        "OPTIONAL" => Mfa.OPTIONAL,
        "REQUIRED" => Mfa.REQUIRED,
        _ => throw new InvalidOperationException($"Unsupported MFA mode: {CognitoConstants.MfaMode}"),
    };
}
