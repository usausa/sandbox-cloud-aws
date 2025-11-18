using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;

namespace Serverless.IaC.Configuration;

/// <summary>
/// プロジェクト全体で共有する定数値。
/// 環境ごとの差異やチューニング対象は本ファイルに集約し、変更容易性を担保する。
/// 「要定義」項目は本クラスの値を更新することで反映される。
/// </summary>
internal static class ProjectConstants
{
    // ---------------------------------------------------------------------
    // プロジェクト基本情報
    // ---------------------------------------------------------------------

    /// <summary>リソース命名のプレフィックス。</summary>
    public const string ProjectName = "serverless-wasm";

    /// <summary>環境識別子(dev / stg / prd 等)。</summary>
    public const string EnvironmentName = "dev";

    // ---------------------------------------------------------------------
    // AWS アカウント / リージョン
    // ---------------------------------------------------------------------

    /// <summary>主リージョン(API/データ層)。</summary>
    public const string PrimaryRegion = "ap-northeast-1";

    /// <summary>CloudFront/WAF/ACM 用リージョン(固定)。</summary>
    public const string EdgeRegion = "us-east-1";

    /// <summary>デプロイ先 AWS アカウント ID。null の場合は CDK の既定(環境変数)を利用。</summary>
    public const string? AccountId = null;

    // ---------------------------------------------------------------------
    // ドメイン / 証明書
    // ---------------------------------------------------------------------

    /// <summary>Route 53 ホストゾーン名(例: example.com)。</summary>
    public const string HostedZoneName = "example.com";

    /// <summary>サービスを公開する FQDN(例: app.example.com)。</summary>
    public const string ServiceDomainName = "app.example.com";

    // ---------------------------------------------------------------------
    // ネットワーク (VPC)
    // ---------------------------------------------------------------------

    public const string VpcCidr = "10.10.0.0/16";

    /// <summary>利用 AZ 数。</summary>
    public const int VpcMaxAzs = 2;

    /// <summary>NAT Gateway 数(コストと可用性のトレードオフ)。</summary>
    public const int NatGatewayCount = 1;

    // ---------------------------------------------------------------------
    // Lambda 共通設定
    // ---------------------------------------------------------------------

    public static Runtime LambdaRuntime => Runtime.DOTNET_8;

    public static Architecture LambdaArchitecture => Architecture.ARM_64;

    public const int LambdaMemorySizeMb = 512;

    public const int LambdaTimeoutSeconds = 15;

    /// <summary>予約済み同時実行数(null で無制限)。</summary>
    public static int? LambdaReservedConcurrency => null;

    // ---------------------------------------------------------------------
    // ログ保持期間
    // ---------------------------------------------------------------------

    public static RetentionDays LambdaLogRetention => RetentionDays.ONE_MONTH;

    public static RetentionDays ApiGatewayLogRetention => RetentionDays.ONE_MONTH;

    public static RetentionDays WafLogRetention => RetentionDays.THREE_MONTHS;

    // ---------------------------------------------------------------------
    // S3 ライフサイクル(日数)
    // ---------------------------------------------------------------------

    public const int SpaOldVersionExpirationDays = 30;

    public const int AuditLogTransitionToGlacierDays = 90;

    public const int AuditLogExpirationDays = 365 * 7;

    public const int SamArtifactExpirationDays = 30;

    // ---------------------------------------------------------------------
    // API Gateway / 認証
    // ---------------------------------------------------------------------

    /// <summary>JWT Issuer (発行元)。要定義。</summary>
    public const string JwtIssuer = "https://issuer.example.com";

    /// <summary>JWT Audience。要定義。</summary>
    public const string JwtAudience = "serverless-wasm-api";

    /// <summary>API パス プレフィックス。</summary>
    public const string ApiPathPrefix = "/api";

    /// <summary>CloudFront → API Gateway 直叩き抑止用カスタムヘッダー名。</summary>
    public const string CloudFrontOriginVerifyHeaderName = "X-Origin-Verify";

    /// <summary>SSM Parameter Store のパス階層プレフィックス。</summary>
    public static string SsmParameterPrefix => $"/{ProjectName}/{EnvironmentName}";

    // ---------------------------------------------------------------------
    // DynamoDB
    // ---------------------------------------------------------------------

    /// <summary>PITR (ポイントインタイムリカバリ) を有効化するか。</summary>
    public const bool DynamoDbPitrEnabled = true;

    // ---------------------------------------------------------------------
    // WAF / レート制限
    // ---------------------------------------------------------------------

    /// <summary>WAF レート制限ルール: 5 分間あたりのリクエスト上限(同一 IP)。</summary>
    public const int WafRateLimitPer5Minutes = 2000;

    // ---------------------------------------------------------------------
    // 通知 (SNS)
    // ---------------------------------------------------------------------

    /// <summary>運用通知メールアドレス。</summary>
    public const string OperatorEmail = "ops@example.com";

    // ---------------------------------------------------------------------
    // CloudWatch Alarm 閾値
    // ---------------------------------------------------------------------

    public const int LambdaErrorAlarmThreshold = 5;

    public const double LambdaDurationAlarmRatio = 0.8;

    public const int LambdaThrottleAlarmThreshold = 1;

    public const int DynamoDbThrottleAlarmThreshold = 1;

    public const double CloudFront5xxAlarmRatePercent = 5.0;

    // ---------------------------------------------------------------------
    // ヘルパー
    // ---------------------------------------------------------------------

    /// <summary>リソース名のサフィックス付与に使用する標準サフィックス。</summary>
    public static string ResourceSuffix => $"{ProjectName}-{EnvironmentName}";

    /// <summary>主リージョン用 <see cref="Environment"/> を取得する。</summary>
    public static Amazon.CDK.Environment GetPrimaryEnvironment() => new()
    {
        Account = AccountId ?? System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
        Region = PrimaryRegion,
    };

    /// <summary>エッジ(us-east-1)用 <see cref="Environment"/> を取得する。</summary>
    public static Amazon.CDK.Environment GetEdgeEnvironment() => new()
    {
        Account = AccountId ?? System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
        Region = EdgeRegion,
    };
}
