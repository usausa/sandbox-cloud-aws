// <copyright file="ConfigConstants.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Constants;

internal static class ConfigConstants
{
    public const string AppEnvironmentParameterName = $"{ParameterPathRoot}/app/environment";

    public const string ParameterPathRoot = $"/serverless/{AppConstants.EnvironmentName}";
}
