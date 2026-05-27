// <copyright file="ObservabilityConstants.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Constants;

internal static class ObservabilityConstants
{
    public const string AlarmEmail = "alerts@example.com";

    public const string AlarmTopicName = $"{AppConstants.ProjectName}-{AppConstants.EnvironmentName}-Alarms";
}
