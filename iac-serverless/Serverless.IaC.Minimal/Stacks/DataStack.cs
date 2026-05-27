// <copyright file="DataStack.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Minimal.Stacks;

using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;
using Serverless.IaC.Minimal.Constants;

internal sealed class DataStack : Stack
{
    public DataStack(Construct scope, string id, IStackProps? props = null)
        : base(scope, id, props)
    {
        this.SampleTable = new TableV2(this, nameof(this.SampleTable), new TablePropsV2
        {
            TableName = DynamoDbConstants.SampleTableName,
            PartitionKey = new Attribute
            {
                Name = DynamoDbConstants.PartitionKeyName,
                Type = AttributeType.STRING,
            },
            SortKey = new Attribute
            {
                Name = DynamoDbConstants.SortKeyName,
                Type = AttributeType.STRING,
            },
            Billing = Billing.OnDemand(),
            PointInTimeRecoverySpecification = new PointInTimeRecoverySpecification
            {
                PointInTimeRecoveryEnabled = DynamoDbConstants.PointInTimeRecoveryEnabled,
            },
            DeletionProtection = DynamoDbConstants.DeletionProtectionEnabled,
            RemovalPolicy = DynamoDbConstants.RemovalPolicy,
            TimeToLiveAttribute = DynamoDbConstants.TtlAttributeName,
        });

        _ = new CfnOutput(this, "SampleTableName", new CfnOutputProps
        {
            Description = "DynamoDB table name",
            Value = this.SampleTable.TableName,
        });
    }

    public ITableV2 SampleTable { get; }
}
