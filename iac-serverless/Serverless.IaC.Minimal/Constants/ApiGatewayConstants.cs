// <copyright file="ApiGatewayConstants.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Minimal.Constants;

using Amazon.CDK.AWS.Logs;

internal static class ApiGatewayConstants
{
    public const string AccessLogFormatJson = "{\"requestId\":\"$context.requestId\",\"ip\":\"$context.identity.sourceIp\",\"requestTime\":\"$context.requestTime\",\"httpMethod\":\"$context.httpMethod\",\"routeKey\":\"$context.routeKey\",\"status\":\"$context.status\",\"protocol\":\"$context.protocol\",\"responseLength\":\"$context.responseLength\"}";

    public const string CloudFrontOriginHeaderName = "X-Origin-Verify";

    public const string CloudFrontOriginHeaderValue = "00000000-0000-0000-0000-000000000000";

    public const string HealthPath = "/api/health";

    public const string JwtAudience = "serverless-api";

    public const string JwtIssuer = "https://example.auth0.com/";

    public const string SamplePath = "/api/sample";

    public const string StageName = "$default";

    public const RetentionDays AccessLogRetentionDays = RetentionDays.ONE_MONTH;
}
