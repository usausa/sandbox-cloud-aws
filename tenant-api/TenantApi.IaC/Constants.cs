namespace TenantApi.IaC;

/// <summary>
/// Centralized configuration values for the stack.
/// Adjust here to rename resources, change region/AZ, or tune sizing without
/// touching the construct code.
/// </summary>
internal static class Constants
{
    // ----- Stack / Deployment -----
    public const string StackName = "TenantApiStack";
    public const string AppName = "tenant-api";
    public const string Region = "ap-northeast-1";
    public const string AvailabilityZone = "ap-northeast-1a";

    // ----- Network -----
    public const string VpcCidr = "10.0.0.0/16";
    public const int VpcMaxAzs = 1;

    // ----- Database (Aurora Serverless v2 PostgreSQL) -----
    public const string DbDefaultDatabaseName = "mydb";
    public const string DbAdminUserName = "postgres";
    public const double DbMinAcu = 0.5;
    public const double DbMaxAcu = 1.0;
    public const int DbBackupRetentionDays = 1;
    public const int DbPort = 5432;

    // ----- ECS / Container -----
    public const int ContainerCpu = 256;
    public const int ContainerMemoryMiB = 512;
    public const int ContainerPort = 8080;
    public const int DesiredCount = 1;
    public const string EcsClusterName = "tenant-api-cluster";
    public const string EcsServiceName = "tenant-api-service";
    public const string ContainerName = "tenant-api";
    public const string DockerBuildContext = "."; // repo root
    public const string DockerfilePath = "TenantApi.Server/Dockerfile";

    // ----- Cloud Map -----
    public const string CloudMapNamespace = "myapp.local";
    public const string CloudMapServiceName = "api";

    // ----- Logging -----
    public const string LogGroupName = "/ecs/tenant-api";
    public const int LogRetentionDays = 7;

    // ----- Cognito -----
    public const string UserPoolName = "tenant-api-user-pool";
    public const string UserPoolClientName = "tenant-api-client";
    public const string TenantIdAttributeName = "tenant_id"; // becomes custom:tenant_id

    // ----- API Gateway -----
    public const string HttpApiName = "tenant-api-http";
    public const string ApiRoutePath = "/tenant";
    public const string TenantHeaderName = "X-Tenant-Id";
    public const string UserSubHeaderName = "X-User-Sub";
    public const string TenantClaimKey = "custom:tenant_id";
    public const string UserSubClaimKey = "sub";

    // ----- Lambda asset paths (relative to cdk.json directory) -----
    public const string PreTokenLambdaPath = "../lambda/pre-token-generation";
    public const string StartLambdaPath = "../lambda/start-environment";
    public const string StopLambdaPath = "../lambda/stop-environment";

    // ----- Schedules (cron in UTC; JST = UTC+9) -----
    // Aurora 起動: 平日 09:00 JST = 00:00 UTC
    public const string ScheduleAuroraStart = "cron(0 0 ? * MON-FRI *)";
    // ECS 起動: 平日 09:05 JST = 00:05 UTC
    public const string ScheduleEcsStart = "cron(5 0 ? * MON-FRI *)";
    // ECS 停止: 平日 21:00 JST = 12:00 UTC
    public const string ScheduleEcsStop = "cron(0 12 ? * MON-FRI *)";
    // Aurora 停止: 平日 21:01 JST = 12:01 UTC
    public const string ScheduleAuroraStop = "cron(1 12 ? * MON-FRI *)";
    public const string ScheduleTimeZone = "UTC";
}
