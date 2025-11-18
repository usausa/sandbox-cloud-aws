using Amazon.CDK;
using Amazon.CDK.AWS.CloudTrail;
using Amazon.CDK.AWS.S3;

using Constructs;

using Serverless.IaC.Configuration;

namespace Serverless.IaC.Stacks;

internal sealed class AuditStackProps : StackProps
{
    public required IBucket AuditLogBucket { get; init; }
}

/// <summary>
/// CloudTrail(マルチリージョン、ログファイル検証有効)を監査ログ用 S3 へ送付。
/// </summary>
internal sealed class AuditStack : Stack
{
    public AuditStack(Construct scope, string id, AuditStackProps props)
        : base(scope, id, props)
    {
        _ = new Trail(this, "ManagementTrail", new TrailProps
        {
            TrailName = $"{ProjectConstants.ResourceSuffix}-trail",
            Bucket = props.AuditLogBucket,
            IsMultiRegionTrail = true,
            IncludeGlobalServiceEvents = true,
            EnableFileValidation = true,
            ManagementEvents = ReadWriteType.ALL,
        });
    }
}
