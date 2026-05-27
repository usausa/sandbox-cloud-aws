// <copyright file="ConfigStack.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Stacks;

using Amazon.CDK;
using Amazon.CDK.AWS.SSM;
using Constructs;
using Serverless.IaC.Constants;

internal sealed class ConfigStack : Stack
{
    public ConfigStack(Construct scope, string id, IStackProps? props = null)

        : base(scope, id, props)
    {
        _ = new StringParameter(this, "AppEnvironmentParameter", new StringParameterProps
        {
            ParameterName = ConfigConstants.AppEnvironmentParameterName,

            StringValue = AppConstants.EnvironmentName,

            Description = "Application environment name",
        });
    }

    public string ParameterPathRoot { get; } = ConfigConstants.ParameterPathRoot;
}
