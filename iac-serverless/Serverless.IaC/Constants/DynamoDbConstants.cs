// <copyright file="DynamoDbConstants.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Constants;

using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;

internal static class DynamoDbConstants
{
    public const bool DeletionProtectionEnabled = true;
    public const string PartitionKeyName = "PK";
    public const bool PointInTimeRecoveryEnabled = true;
    public const string SampleTableName = "Serverless-Sample";
    public const string SortKeyName = "SK";
    public const string? TtlAttributeName = null;
    public static readonly BillingMode BillingMode = BillingMode.PAY_PER_REQUEST;
    public static readonly RemovalPolicy RemovalPolicy = RemovalPolicy.RETAIN;
}
