using Amazon.CDK;
using Amazon.CDK.AWS.S3;

using Constructs;

using Serverless.IaC.Configuration;

namespace Serverless.IaC.Stacks;

/// <summary>
/// S3 バケット群: SPA 配信 / 監査ログ / SAM デプロイ用アーティファクト。
/// 全バケットでパブリックアクセスをブロックし、用途に応じてライフサイクルを設定する。
/// </summary>
internal sealed class StorageStack : Stack
{
    public StorageStack(Construct scope, string id, IStackProps props)
        : base(scope, id, props)
    {
        SpaBucket = new Bucket(this, "SpaBucket", new BucketProps
        {
            BucketName = $"{ProjectConstants.ResourceSuffix}-spa",
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            Encryption = BucketEncryption.S3_MANAGED,
            Versioned = true,
            EnforceSSL = true,
            RemovalPolicy = RemovalPolicy.RETAIN,
            LifecycleRules =
            [
                new LifecycleRule
                {
                    Id = "ExpireOldVersions",
                    NoncurrentVersionExpiration = Duration.Days(ProjectConstants.SpaOldVersionExpirationDays),
                },
            ],
        });

        AuditLogBucket = new Bucket(this, "AuditLogBucket", new BucketProps
        {
            BucketName = $"{ProjectConstants.ResourceSuffix}-audit",
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            Encryption = BucketEncryption.S3_MANAGED,
            Versioned = true,
            EnforceSSL = true,
            RemovalPolicy = RemovalPolicy.RETAIN,
            LifecycleRules =
            [
                new LifecycleRule
                {
                    Id = "TransitionToGlacier",
                    Transitions =
                    [
                        new Transition
                        {
                            StorageClass = StorageClass.GLACIER,
                            TransitionAfter = Duration.Days(ProjectConstants.AuditLogTransitionToGlacierDays),
                        },
                    ],
                    Expiration = Duration.Days(ProjectConstants.AuditLogExpirationDays),
                },
            ],
        });

        SamArtifactBucket = new Bucket(this, "SamArtifactBucket", new BucketProps
        {
            BucketName = $"{ProjectConstants.ResourceSuffix}-sam-artifacts",
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            Encryption = BucketEncryption.S3_MANAGED,
            EnforceSSL = true,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true,
            LifecycleRules =
            [
                new LifecycleRule
                {
                    Id = "ExpireOldArtifacts",
                    Expiration = Duration.Days(ProjectConstants.SamArtifactExpirationDays),
                },
            ],
        });
    }

    public IBucket SpaBucket { get; }

    public IBucket AuditLogBucket { get; }

    public IBucket SamArtifactBucket { get; }
}
