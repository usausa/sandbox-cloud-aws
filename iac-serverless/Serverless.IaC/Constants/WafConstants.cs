// <copyright file="WafConstants.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Constants;

internal static class WafConstants
{
    public const int AnonymousIpPriority = 20;

    public const int CommonRuleSetPriority = 40;

    public const int IpReputationPriority = 10;

    public const int KnownBadInputsPriority = 30;

    public const int RateLimitPer5Min = 2000;

    public const int RateLimitPriority = 100;

    public const int SqliPriority = 50;

    public const string Scope = "CLOUDFRONT";
}
