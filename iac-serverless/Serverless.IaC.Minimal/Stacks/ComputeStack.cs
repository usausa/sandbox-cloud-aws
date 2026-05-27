// <copyright file="ComputeStack.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Minimal.Stacks;

using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Constructs;
using Serverless.IaC.Minimal.Constants;

internal sealed class ComputeStack : Stack
{
    public ComputeStack(Construct scope, string id, ITableV2 sampleTable, IStackProps? props = null)
        : base(scope, id, props)
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

        this.SampleFunction = new Function(this, nameof(this.SampleFunction), new FunctionProps
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
            Environment = new Dictionary<string, string>
            {
                [LambdaConstants.EnvironmentNameVariable] = AppConstants.EnvironmentName,
                [LambdaConstants.TableNameVariable] = sampleTable.TableName,
            },
        });
    }

    public IFunction SampleFunction { get; }
}
