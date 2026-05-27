// <copyright file="S3Constants.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Minimal.Constants;

internal static class S3Constants
{
    public const string BucketNamePrefix = "serverlessminimal-dev";
    public const string SamArtifactBucketSuffix = "-sam";
    public const int SamArtifactExpirationDays = 30;
    public const string SpaBucketSuffix = "-spa";
    public const int SpaOldVersionExpirationDays = 30;
}
