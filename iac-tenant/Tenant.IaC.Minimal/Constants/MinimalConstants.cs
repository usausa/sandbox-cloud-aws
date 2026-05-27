namespace Tenant.IaC.Minimal.Constants;

internal static class MinimalConstants
{
    // TODO: 要定義 - デプロイ先リージョンを変更すること
    public const string Region = "ap-northeast-1";

    // TODO: 要定義 - 本番デプロイ時にプレフィックスを変更すること
    public const string StackPrefix = "TenantMinimal";

    // Tags
    // TODO: 要定義 - プロジェクト名タグを変更すること
    public const string TagProject = "Tenant";

    // TODO: 要定義 - 本番環境では "prod" などに変更すること
    public const string TagEnvironment = "dev";

    public const string TagManagedBy = "CDK";

    // Network
    // フル版 (10.0.0.0/16) との衝突を避けるため 10.1 系を使用
    public const string VpcCidr = "10.1.0.0/16";
    public const int MaxAzs = 2;
    public const int ContainerPort = 8080;
    public const int AlbPort = 80;

    // ECR
    public const string EcrRepositoryName = "tenant-ping";
    public const int EcrImageRetentionCount = 5;

    // ECS
    public const string ClusterName = "tenant-ping-cluster";
    public const string ServiceName = "tenant-ping-service";
    public const string TaskFamily = "tenant-ping-task";
    public const string ContainerName = "tenant-ping";
    public const string LogGroupName = "/ecs/tenant-ping";
    public const int CpuUnits = 256;
    public const int MemoryMiB = 512;
    public const int DesiredCount = 1;
    public const int LogRetentionDays = 7;
    public const string ImageTag = "latest";
    public const string HealthCheckPath = "/ping";

    // API Gateway
    public const string ApiName = "tenant-ping-api";
    public const string AlbName = "tenant-ping-alb";
}
