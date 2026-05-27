// <copyright file="LambdaConstants.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Minimal.Constants;

using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;

internal static class LambdaConstants
{
    public const string EnvironmentNameVariable = "ENVIRONMENT";
    public const string PlaceholderAssetPath = @"src\placeholder";
    public const string TableNameVariable = "TABLE_NAME";
    public const int MemorySize = 512;
    public const RetentionDays LogRetentionDays = RetentionDays.ONE_MONTH;
    public static readonly Architecture Architecture = Architecture.ARM_64;
    public static readonly Runtime DotnetRuntime = Runtime.DOTNET_10;
    public static readonly Duration Timeout = Duration.Seconds(30);

    public static int? ReservedConcurrentExecutions => null;
}
