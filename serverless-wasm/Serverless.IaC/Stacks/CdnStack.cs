using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.Route53.Targets;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.WAFv2;

using Constructs;

using Serverless.IaC.Configuration;

namespace Serverless.IaC.Stacks;

internal sealed class CdnStackProps : StackProps
{
    public required IBucket SpaBucket { get; init; }

    public required HttpApi HttpApi { get; init; }

    public required ICertificate Certificate { get; init; }

    public required CfnWebACL WebAcl { get; init; }

    public required string OriginVerifyHeaderValue { get; init; }
}

/// <summary>
/// CloudFront ディストリビューション、Route 53 ALIAS、レスポンスヘッダーポリシー。
/// SPA は S3 + OAC、API は API Gateway をオリジンに設定する。
/// </summary>
internal sealed class CdnStack : Stack
{
    public CdnStack(Construct scope, string id, CdnStackProps props)
        : base(scope, id, props)
    {
        var responseHeadersPolicy = new ResponseHeadersPolicy(this, "SecurityHeadersPolicy", new ResponseHeadersPolicyProps
        {
            ResponseHeadersPolicyName = $"{ProjectConstants.ResourceSuffix}-sec-headers",
            SecurityHeadersBehavior = new ResponseSecurityHeadersBehavior
            {
                ContentTypeOptions = new ResponseHeadersContentTypeOptions { Override = true },
                FrameOptions = new ResponseHeadersFrameOptions
                {
                    FrameOption = HeadersFrameOption.DENY,
                    Override = true,
                },
                ReferrerPolicy = new ResponseHeadersReferrerPolicy
                {
                    ReferrerPolicy = HeadersReferrerPolicy.STRICT_ORIGIN_WHEN_CROSS_ORIGIN,
                    Override = true,
                },
                StrictTransportSecurity = new ResponseHeadersStrictTransportSecurity
                {
                    AccessControlMaxAge = Duration.Days(365),
                    IncludeSubdomains = true,
                    Preload = true,
                    Override = true,
                },
                XssProtection = new ResponseHeadersXSSProtection
                {
                    Protection = true,
                    ModeBlock = true,
                    Override = true,
                },
            },
        });

        var spaOrigin = S3BucketOrigin.WithOriginAccessControl(props.SpaBucket);

        var apiDomain = $"{props.HttpApi.HttpApiId}.execute-api.{ProjectConstants.PrimaryRegion}.amazonaws.com";
        var apiOrigin = new HttpOrigin(apiDomain, new HttpOriginProps
        {
            ProtocolPolicy = OriginProtocolPolicy.HTTPS_ONLY,
            CustomHeaders = new System.Collections.Generic.Dictionary<string, string>
            {
                [ProjectConstants.CloudFrontOriginVerifyHeaderName] = props.OriginVerifyHeaderValue,
            },
        });

        Distribution = new Distribution(this, "Distribution", new DistributionProps
        {
            Comment = $"{ProjectConstants.ResourceSuffix} distribution",
            DomainNames = [ProjectConstants.ServiceDomainName],
            Certificate = props.Certificate,
            WebAclId = props.WebAcl.AttrArn,
            HttpVersion = HttpVersion.HTTP2_AND_3,
            MinimumProtocolVersion = SecurityPolicyProtocol.TLS_V1_2_2021,
            DefaultRootObject = "index.html",

            // SPA(S3)既定ビヘイビア
            DefaultBehavior = new BehaviorOptions
            {
                Origin = spaOrigin,
                ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                AllowedMethods = AllowedMethods.ALLOW_GET_HEAD_OPTIONS,
                CachePolicy = CachePolicy.CACHING_OPTIMIZED,
                ResponseHeadersPolicy = responseHeadersPolicy,
                Compress = true,
            },

            AdditionalBehaviors = new System.Collections.Generic.Dictionary<string, IBehaviorOptions>
            {
                // API: キャッシュ無効
                [$"{ProjectConstants.ApiPathPrefix}/*"] = new BehaviorOptions
                {
                    Origin = apiOrigin,
                    ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                    AllowedMethods = AllowedMethods.ALLOW_ALL,
                    CachePolicy = CachePolicy.CACHING_DISABLED,
                    OriginRequestPolicy = OriginRequestPolicy.ALL_VIEWER_EXCEPT_HOST_HEADER,
                    ResponseHeadersPolicy = responseHeadersPolicy,
                },
            },

            ErrorResponses =
            [
                new ErrorResponse
                {
                    HttpStatus = 403,
                    ResponseHttpStatus = 200,
                    ResponsePagePath = "/index.html",
                    Ttl = Duration.Seconds(0),
                },
                new ErrorResponse
                {
                    HttpStatus = 404,
                    ResponseHttpStatus = 200,
                    ResponsePagePath = "/index.html",
                    Ttl = Duration.Seconds(0),
                },
            ],
        });

        // Route 53 ALIAS
        var hostedZone = HostedZone.FromLookup(this, "HostedZone", new HostedZoneProviderProps
        {
            DomainName = ProjectConstants.HostedZoneName,
        });

        _ = new ARecord(this, "AliasRecord", new ARecordProps
        {
            Zone = hostedZone,
            RecordName = ProjectConstants.ServiceDomainName,
            Target = RecordTarget.FromAlias(new CloudFrontTarget(Distribution)),
        });
    }

    public IDistribution Distribution { get; }
}
