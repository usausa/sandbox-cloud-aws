// <copyright file="CloudFrontConstants.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Constants;

using Amazon.CDK.AWS.CloudFront;

internal static class CloudFrontConstants
{
    public const string ApiPathPattern = "/api/*";

    public const string DefaultRootObject = "index.html";

    public const string SpaAssetPathPattern = "/assets/*";

    public const string SpaFallbackPath = "/index.html";

    public const int HstsMaxAge = 31536000;

    public const bool HstsIncludeSubdomains = true;

    public const bool HstsPreload = true;

    public const int SpaAssetTtlDefault = 86400;

    public const int SpaAssetTtlMax = 31536000;

    public const int SpaFallbackHttpStatus = 200;

    public const int SpaMainTtlDefault = 60;

    public const int SpaMainTtlMax = 300;

    public const int SpaMainTtlMin = 0;

    public const SecurityPolicyProtocol MinimumProtocolVersion = SecurityPolicyProtocol.TLS_V1_2_2021;

    public static readonly PriceClass PriceClass = PriceClass.PRICE_CLASS_200;
}
