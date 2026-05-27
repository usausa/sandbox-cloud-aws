// <copyright file="AuditStack.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Stacks;

using Amazon.CDK;
using Amazon.CDK.AWS.CloudTrail;
using Amazon.CDK.AWS.S3;
using Constructs;
using Serverless.IaC.Constants;

internal sealed class AuditStack : Stack
{
    public AuditStack(Construct scope, string id, IBucket auditBucket, IStackProps? props = null)

        : base(scope, id, props)
    {
        var trail = new Trail(this, "CloudTrail", new TrailProps
        {
            TrailName = AuditConstants.TrailName,

            Bucket = auditBucket,

            IsMultiRegionTrail = AuditConstants.IsMultiRegionTrail,

            EnableFileValidation = AuditConstants.EnableFileValidation,

            IncludeGlobalServiceEvents = true,

            ManagementEvents = ReadWriteType.ALL,
        });

        _ = new CfnOutput(this, "TrailArn", new CfnOutputProps
        {
            Description = "CloudTrail ARN",

            Value = trail.TrailArn,
        });
    }
}
