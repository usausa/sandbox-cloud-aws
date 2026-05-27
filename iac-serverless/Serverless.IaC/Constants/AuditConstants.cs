// <copyright file="AuditConstants.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Constants;

internal static class AuditConstants
{
    public const bool EnableFileValidation = true;

    public const bool EnableS3DataEvents = false;

    public const bool IsMultiRegionTrail = true;

    public const string TrailName = $"{AppConstants.ProjectName}-{AppConstants.EnvironmentName}-Trail";
}
