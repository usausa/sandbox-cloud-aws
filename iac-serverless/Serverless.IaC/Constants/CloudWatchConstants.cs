// <copyright file="CloudWatchConstants.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Constants;

using Amazon.CDK.AWS.Logs;

internal static class CloudWatchConstants
{
    public const string CloudFrontErrorRateMetricName = "5xxErrorRate";

    public const double CloudFront5xxThreshold = 5.0;

    public const string DashboardName = $"{AppConstants.ProjectName}-{AppConstants.EnvironmentName}-Dashboard";

    public const string DynamoDbNamespace = "AWS/DynamoDB";

    public const string DynamoThrottledRequestsMetricName = "ThrottledRequests";

    public const int DynamoThrottleThreshold = 1;

    public const string HttpApi5xxMetricName = "5xx";

    public const int HttpApi5xxThreshold = 5;

    public const string HttpApiNamespace = "AWS/ApiGateway";

    public const int LambdaDurationThresholdMs = 24000;

    public const int LambdaErrorsThreshold = 5;

    public const int LambdaThrottlesThreshold = 1;

    public const string GlobalRegion = "Global";

    public const RetentionDays LogRetentionDays = RetentionDays.ONE_MONTH;
}
