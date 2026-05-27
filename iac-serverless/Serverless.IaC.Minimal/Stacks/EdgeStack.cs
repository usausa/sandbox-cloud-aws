// <copyright file="EdgeStack.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Minimal.Stacks;

using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.S3;
using Constructs;
using Serverless.IaC.Minimal.Constants;

internal sealed class EdgeStack : Stack
{
    public EdgeStack(Construct scope, string id, IBucket spaBucket, string apiDomainName, ICertificate certificate, string webAclArn, IStackProps? props = null)
        : base(scope, id, props)
    {
        var spaMainCachePolicy = new CachePolicy(this, "SpaMainCachePolicy", new CachePolicyProps
        {
            CachePolicyName = $"{AppConstants.StackPrefix}-SpaMain",
            DefaultTtl = Duration.Seconds(CloudFrontConstants.SpaMainTtlDefault),
            MinTtl = Duration.Seconds(CloudFrontConstants.SpaMainTtlMin),
            MaxTtl = Duration.Seconds(CloudFrontConstants.SpaMainTtlMax),
        });
        var spaAssetCachePolicy = new CachePolicy(this, "SpaAssetCachePolicy", new CachePolicyProps
        {
            CachePolicyName = $"{AppConstants.StackPrefix}-SpaAssets",
            DefaultTtl = Duration.Seconds(CloudFrontConstants.SpaAssetTtlDefault),
            MinTtl = Duration.Seconds(CloudFrontConstants.SpaMainTtlMin),
            MaxTtl = Duration.Seconds(CloudFrontConstants.SpaAssetTtlMax),
        });
        var responseHeadersPolicy = this.CreateResponseHeadersPolicy();
        var apiOrigin = new HttpOrigin(apiDomainName, new HttpOriginProps
        {
            ProtocolPolicy = OriginProtocolPolicy.HTTPS_ONLY,
            CustomHeaders = new Dictionary<string, string>
            {
                [ApiGatewayConstants.CloudFrontOriginHeaderName] = ApiGatewayConstants.CloudFrontOriginHeaderValue,
            },
        });
        var spaOrigin = S3BucketOrigin.WithOriginAccessControl(spaBucket);

        this.Distribution = new Distribution(this, nameof(this.Distribution), new DistributionProps
        {
            Comment = $"{AppConstants.StackPrefix} CloudFront distribution",
            Certificate = certificate,
            DefaultRootObject = CloudFrontConstants.DefaultRootObject,
            DomainNames = [DomainConstants.SiteDomain],
            MinimumProtocolVersion = CloudFrontConstants.MinimumProtocolVersion,
            HttpVersion = HttpVersion.HTTP2_AND_3,
            PriceClass = CloudFrontConstants.PriceClass,
            WebAclId = webAclArn,
            DefaultBehavior = new BehaviorOptions
            {
                Origin = spaOrigin,
                ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                AllowedMethods = AllowedMethods.ALLOW_GET_HEAD_OPTIONS,
                CachePolicy = spaMainCachePolicy,
                ResponseHeadersPolicy = responseHeadersPolicy,
                Compress = true,
            },
            AdditionalBehaviors = new Dictionary<string, IBehaviorOptions>
            {
                [CloudFrontConstants.ApiPathPattern] = new BehaviorOptions
                {
                    Origin = apiOrigin,
                    ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                    CachePolicy = CachePolicy.CACHING_DISABLED,
                    OriginRequestPolicy = OriginRequestPolicy.ALL_VIEWER_EXCEPT_HOST_HEADER,
                    AllowedMethods = AllowedMethods.ALLOW_ALL,
                },
                [CloudFrontConstants.SpaAssetPathPattern] = new BehaviorOptions
                {
                    Origin = spaOrigin,
                    ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                    AllowedMethods = AllowedMethods.ALLOW_GET_HEAD_OPTIONS,
                    CachePolicy = spaAssetCachePolicy,
                    ResponseHeadersPolicy = responseHeadersPolicy,
                    Compress = true,
                },
            },
            ErrorResponses =
            [
                new ErrorResponse { HttpStatus = 403, ResponseHttpStatus = CloudFrontConstants.SpaFallbackHttpStatus, ResponsePagePath = CloudFrontConstants.SpaFallbackPath, Ttl = Duration.Seconds(0) },
                new ErrorResponse { HttpStatus = 404, ResponseHttpStatus = CloudFrontConstants.SpaFallbackHttpStatus, ResponsePagePath = CloudFrontConstants.SpaFallbackPath, Ttl = Duration.Seconds(0) },
            ],
        });

        _ = new CfnOutput(this, "DistributionDomainName", new CfnOutputProps { Description = "CloudFront distribution domain name", Value = this.Distribution.DistributionDomainName });
    }

    public IDistribution Distribution { get; }

    private ResponseHeadersPolicy CreateResponseHeadersPolicy()
    {
        return new ResponseHeadersPolicy(this, "ResponseHeadersPolicy", new ResponseHeadersPolicyProps
        {
            ResponseHeadersPolicyName = $"{AppConstants.StackPrefix}-ResponseHeaders",
            SecurityHeadersBehavior = new ResponseSecurityHeadersBehavior
            {
                ContentTypeOptions = new ResponseHeadersContentTypeOptions { Override = true },
                FrameOptions = new ResponseHeadersFrameOptions { FrameOption = HeadersFrameOption.DENY, Override = true },
                StrictTransportSecurity = new ResponseHeadersStrictTransportSecurity
                {
                    AccessControlMaxAge = Duration.Seconds(CloudFrontConstants.HstsMaxAge),
                    IncludeSubdomains = CloudFrontConstants.HstsIncludeSubdomains,
                    Preload = CloudFrontConstants.HstsPreload,
                    Override = true,
                },
                XssProtection = new ResponseHeadersXSSProtection { Protection = true, ModeBlock = true, Override = true },
            },
        });
    }
}
