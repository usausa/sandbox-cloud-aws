// <copyright file="S3Constants.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Constants;

internal static class S3Constants
{
    public const string BucketNamePrefix = "serverless-dev";
    public const string AuditBucketSuffix = "-audit";
    public const int AuditLogExpirationDays = 365;
    public const int AuditLogGlacierTransitionDays = 90;
    public const string SamArtifactBucketSuffix = "-sam";
    public const int SamArtifactExpirationDays = 30;
    public const string SpaBucketSuffix = "-spa";
    public const int SpaOldVersionExpirationDays = 30;
}
