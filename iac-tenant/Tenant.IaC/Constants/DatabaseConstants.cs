namespace Tenant.IaC.Constants;

internal static class DatabaseConstants
{
    public const string ClusterConstructId = "TenantAuroraCluster";
    public const string WriterInstanceIdentifier = "tenant-aurora-writer";
    public const string ClusterEndpointOutputId = "ClusterEndpoint";
    public const string ClusterPortOutputId = "ClusterPort";
    public const string SecretArnOutputId = "SecretArn";
    public const string ClusterIdentifier = "tenant-aurora";
    public const string DefaultDatabaseName = "appdb";
    public const string AdminUserName = "dbadmin";
    public const string SecretName = "tenant-aurora-credentials";
    public const string SecretUsernameField = "username";
    public const string SecretPasswordField = "password";
    public const double MinAcu = 0.5;
    public const bool StorageEncrypted = true;

    // TODO: 要定義 - 負荷に応じて MaxAcu を調整すること
    public const double MaxAcu = 1.0;

    // TODO: 要定義 - 本番では 7 日以上に変更すること
    public const int BackupRetentionDays = 1;

    // TODO: 要定義 - 本番では true (削除保護有効) に変更すること
    public const bool DeletionProtectionEnabled = false;
}
