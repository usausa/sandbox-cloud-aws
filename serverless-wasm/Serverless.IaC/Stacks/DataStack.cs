using System.Collections.Generic;

using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;

using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;
using Amazon.CDK.AWS.SSM;

using Constructs;

using Serverless.IaC.Configuration;

namespace Serverless.IaC.Stacks;

/// <summary>
/// データ層スタック。DynamoDB テーブルと SSM Parameter Store を集約管理する。
/// テーブル一覧 / キー設計はアプリ要件確定後に <see cref="DefineTables"/> 内で更新する。
/// </summary>
internal sealed class DataStack : Stack
{
    public DataStack(Construct scope, string id, IStackProps props)
        : base(scope, id, props)
    {
        Tables = DefineTables();

        ParameterPrefix = ProjectConstants.SsmParameterPrefix;

        // 共通設定値の例(値はデプロイ後に Parameter Store 上で運用更新する想定)
        _ = new StringParameter(this, "JwtIssuerParam", new StringParameterProps
        {
            ParameterName = $"{ParameterPrefix}/auth/jwt-issuer",
            StringValue = ProjectConstants.JwtIssuer,
            Tier = ParameterTier.STANDARD,
        });

        _ = new StringParameter(this, "JwtAudienceParam", new StringParameterProps
        {
            ParameterName = $"{ParameterPrefix}/auth/jwt-audience",
            StringValue = ProjectConstants.JwtAudience,
            Tier = ParameterTier.STANDARD,
        });
    }

    public IReadOnlyList<ITable> Tables { get; }

    public string ParameterPrefix { get; }

    private IReadOnlyList<ITable> DefineTables()
    {
        // TODO: アプリ要件確定後にテーブル定義を追加
        var sample = new Table(this, "AppTable", new TableProps
        {
            TableName = $"{ProjectConstants.ResourceSuffix}-app",
            PartitionKey = new Attribute { Name = "PK", Type = AttributeType.STRING },
            SortKey = new Attribute { Name = "SK", Type = AttributeType.STRING },
            BillingMode = BillingMode.PAY_PER_REQUEST,
            PointInTimeRecoverySpecification = new PointInTimeRecoverySpecification
            {
                PointInTimeRecoveryEnabled = ProjectConstants.DynamoDbPitrEnabled,
            },
            RemovalPolicy = RemovalPolicy.RETAIN,
        });

        return [sample];
    }
}
