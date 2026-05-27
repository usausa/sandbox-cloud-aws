// <copyright file="StorageStack.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Minimal.Stacks;

using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Constructs;
using Serverless.IaC.Minimal.Constants;

internal sealed class StorageStack : Stack
{
    public StorageStack(Construct scope, string id, IStackProps? props = null)
        : base(scope, id, props)
    {
        this.SpaBucket = this.CreateSpaBucket();
        this.SamArtifactBucket = this.CreateSamArtifactBucket();

        _ = new CfnOutput(this, "SpaBucketName", new CfnOutputProps { Description = "SPA bucket name", Value = this.SpaBucket.BucketName });
        _ = new CfnOutput(this, "SamArtifactBucketName", new CfnOutputProps { Description = "SAM artifact bucket name", Value = this.SamArtifactBucket.BucketName });
    }

    public IBucket SamArtifactBucket { get; }

    public IBucket SpaBucket { get; }

    private Bucket CreateSamArtifactBucket()
    {
        return new Bucket(this, nameof(this.SamArtifactBucket), new BucketProps
        {
            BucketName = $"{S3Constants.BucketNamePrefix}{S3Constants.SamArtifactBucketSuffix}",
            Versioned = true,
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            EnforceSSL = true,
            RemovalPolicy = RemovalPolicy.RETAIN,
            LifecycleRules =
            [
                new LifecycleRule
                {
                    Id = "ExpireSamArtifacts",
                    Enabled = true,
                    NoncurrentVersionExpiration = Duration.Days(S3Constants.SamArtifactExpirationDays),
                },
            ],
        });
    }

    private Bucket CreateSpaBucket()
    {
        return new Bucket(this, nameof(this.SpaBucket), new BucketProps
        {
            BucketName = $"{S3Constants.BucketNamePrefix}{S3Constants.SpaBucketSuffix}",
            Versioned = true,
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            EnforceSSL = true,
            RemovalPolicy = RemovalPolicy.RETAIN,
            LifecycleRules =
            [
                new LifecycleRule
                {
                    Id = "ExpireOldSpaVersions",
                    Enabled = true,
                    NoncurrentVersionExpiration = Duration.Days(S3Constants.SpaOldVersionExpirationDays),
                },
            ],
        });
    }
}
