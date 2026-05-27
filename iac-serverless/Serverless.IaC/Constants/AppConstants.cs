// <copyright file="AppConstants.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Constants;

internal static class AppConstants
{
    public const string ProjectName = "Serverless";

    public const string EnvironmentName = "dev";

    public const string StackPrefix = $"{ProjectName}-{EnvironmentName}";

    public const string PrimaryRegion = "ap-northeast-1";

    public const string CloudFrontCertificateRegion = "us-east-1";
}
