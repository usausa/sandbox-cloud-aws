// <copyright file="ComputeStack.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Stacks;

using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Constructs;
using Serverless.IaC.Constants;

internal sealed class ComputeStack : Stack
{
    public ComputeStack(Construct scope, string id, IVpc vpc, ISecurityGroup lambdaSecurityGroup, ITableV2 sampleTable, string parameterPathRoot, IStackProps? props = null)
        : base(scope, id, props)
    {
        this.SampleFunction = this.CreateSampleFunction(sampleTable);
        this.SampleVpcFunction = this.CreateSampleVpcFunction(vpc, lambdaSecurityGroup, sampleTable, parameterPathRoot);
    }

    public IFunction SampleFunction { get; }

    public IFunction SampleVpcFunction { get; }

    private static Dictionary<string, string> CreateBaseEnvironment(ITableV2 sampleTable)
    {
        return new Dictionary<string, string>
        {
            [LambdaConstants.EnvironmentNameVariable] = AppConstants.EnvironmentName,
            [LambdaConstants.TableNameVariable] = sampleTable.TableName,
        };
    }

    private Function CreateSampleFunction(ITableV2 sampleTable)
    {
        var role = new Role(this, "SampleFunctionRole", new RoleProps
        {
            AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
            ManagedPolicies = [ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole")],
        });
        sampleTable.GrantReadWriteData(role);

        var logGroup = new LogGroup(this, "SampleFunctionLogGroup", new LogGroupProps
        {
            LogGroupName = $"/aws/lambda/{AppConstants.StackPrefix}-Sample",
            Retention = LambdaConstants.LogRetentionDays,
            RemovalPolicy = RemovalPolicy.DESTROY,
        });

        return new Function(this, nameof(this.SampleFunction), new FunctionProps
        {
            FunctionName = $"{AppConstants.StackPrefix}-Sample",
            Runtime = LambdaConstants.DotnetRuntime,
            Architecture = LambdaConstants.Architecture,
            Handler = "bootstrap",
            Code = Code.FromAsset(LambdaConstants.PlaceholderAssetPath),
            MemorySize = LambdaConstants.MemorySize,
            Timeout = LambdaConstants.Timeout,
            LogGroup = logGroup,
            Role = role,
            Environment = CreateBaseEnvironment(sampleTable),
        });
    }

    private Function CreateSampleVpcFunction(IVpc vpc, ISecurityGroup lambdaSecurityGroup, ITableV2 sampleTable, string parameterPathRoot)
    {
        var role = new Role(this, "SampleVpcFunctionRole", new RoleProps
        {
            AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
            ManagedPolicies =
            [
                ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"),
                ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaVPCAccessExecutionRole"),
            ],
        });
        sampleTable.GrantReadWriteData(role);
        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["ssm:GetParameter", "ssm:GetParameters", "ssm:GetParametersByPath"],
            Resources =
            [
                Of(this).FormatArn(new ArnComponents
                {
                    Service = "ssm",
                    Resource = "parameter",
                    ResourceName = $"{parameterPathRoot}/*",
                }),
            ],
        }));

        var environment = CreateBaseEnvironment(sampleTable);
        environment[LambdaConstants.ParameterPathRootVariable] = parameterPathRoot;

        var logGroup = new LogGroup(this, "SampleVpcFunctionLogGroup", new LogGroupProps
        {
            LogGroupName = $"/aws/lambda/{AppConstants.StackPrefix}-SampleVpc",
            Retention = LambdaConstants.LogRetentionDays,
            RemovalPolicy = RemovalPolicy.DESTROY,
        });

        return new Function(this, nameof(this.SampleVpcFunction), new FunctionProps
        {
            FunctionName = $"{AppConstants.StackPrefix}-SampleVpc",
            Runtime = LambdaConstants.DotnetRuntime,
            Architecture = LambdaConstants.Architecture,
            Handler = "bootstrap",
            Code = Code.FromAsset(LambdaConstants.PlaceholderAssetPath),
            MemorySize = LambdaConstants.MemorySize,
            Timeout = LambdaConstants.Timeout,
            LogGroup = logGroup,
            Role = role,
            Vpc = vpc,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
            SecurityGroups = [lambdaSecurityGroup],
            Environment = environment,
        });
    }
}
