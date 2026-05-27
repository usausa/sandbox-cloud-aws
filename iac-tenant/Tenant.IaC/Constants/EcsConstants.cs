namespace Tenant.IaC.Constants;

internal static class EcsConstants
{
    public const string RepositoryConstructId = "TenantWebApiRepository";
    public const string ClusterConstructId = "TenantEcsCluster";
    public const string LogGroupConstructId = "TenantWebApiLogGroup";
    public const string TaskDefinitionConstructId = "TenantWebApiTaskDefinition";
    public const string ServiceConstructId = "TenantWebApiService";
    public const string AlbConstructId = "TenantAlb";
    public const string ListenerConstructId = "TenantAlbHttpListener";
    public const string TargetGroupConstructId = "TenantAlbTargetGroup";
    public const string EcrRepositoryUriOutputId = "EcrRepositoryUri";
    public const string ClusterNameOutputId = "ClusterName";
    public const string ServiceNameOutputId = "ServiceName";
    public const string AlbDnsNameOutputId = "AlbDnsName";
    public const string EcrRepositoryName = "tenant-webapi";
    public const int EcrImageRetentionCount = 5;
    public const string ClusterName = "tenant-cluster";
    public const string ServiceName = "tenant-webapi-service";
    public const string TaskFamily = "tenant-webapi-task";
    public const string ContainerName = "tenant-webapi";
    public const string AlbName = "tenant-alb";
    public const string LogGroupName = "/ecs/tenant-webapi";
    public const string LogStreamPrefix = "tenant-webapi";
    public const int CpuUnits = 256;
    public const int MemoryMiB = 512;
    public const int DesiredCount = 1;

    // TODO: 要定義 - 本番ではコミット SHA 等のイミュータブルタグに変更すること
    public const string ImageTag = "latest";

    public const string HealthCheckPath = "/health";
    public const string HealthCheckSuccessCodes = "200";
    public const string FargateSpotCapacityProvider = "FARGATE_SPOT";
    public const int FargateSpotWeight = 1;
    public const string DbHostEnvironmentName = "DB_HOST";
    public const string DbNameEnvironmentName = "DB_NAME";
    public const string AwsRegionEnvironmentName = "AWS_REGION";
    public const string DbUsernameSecretName = "DB_USERNAME";
    public const string DbPasswordSecretName = "DB_PASSWORD";

    // TODO: 要定義 - 本番では 30 日以上を推奨
    public const int LogRetentionDays = 7;
}
