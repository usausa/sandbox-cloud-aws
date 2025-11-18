using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;

using Constructs;

namespace TenantApi.IaC.Constructs;

internal sealed class DatabaseConstructProps
{
    public required Vpc Vpc { get; init; }

    public required SecurityGroup AuroraSecurityGroup { get; init; }
}

/// <summary>
/// Aurora Serverless v2 (PostgreSQL) cluster with a single writer instance.
/// Master credentials are managed by Secrets Manager.
/// </summary>
internal sealed class DatabaseConstruct : Construct
{
    public DatabaseCluster Cluster { get; }

    public string SecretArn { get; }

    public DatabaseConstruct(Construct scope, string id, DatabaseConstructProps props)
        : base(scope, id)
    {
        var engine = DatabaseClusterEngine.AuroraPostgres(new AuroraPostgresClusterEngineProps
        {
            Version = AuroraPostgresEngineVersion.VER_16_4,
        });

        var credentials = Credentials.FromGeneratedSecret(Constants.DbAdminUserName);

        Cluster = new DatabaseCluster(this, "Cluster", new DatabaseClusterProps
        {
            Engine = engine,
            Credentials = credentials,
            DefaultDatabaseName = Constants.DbDefaultDatabaseName,
            Vpc = props.Vpc,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED },
            SecurityGroups = [props.AuroraSecurityGroup],
            ServerlessV2MinCapacity = Constants.DbMinAcu,
            ServerlessV2MaxCapacity = Constants.DbMaxAcu,
            Writer = ClusterInstance.ServerlessV2("Writer", new ServerlessV2ClusterInstanceProps
            {
                PubliclyAccessible = false,
            }),
            Backup = new BackupProps { Retention = Duration.Days(Constants.DbBackupRetentionDays) },
            DeletionProtection = false,
            RemovalPolicy = RemovalPolicy.DESTROY,
            StorageEncrypted = true,
        });

        SecretArn = Cluster.Secret!.SecretArn;
    }
}
