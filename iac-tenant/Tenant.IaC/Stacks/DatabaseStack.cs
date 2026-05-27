namespace Tenant.IaC.Stacks;

using System;
using System.Globalization;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SecretsManager;
using Constructs;
using Tenant.IaC.Constants;

internal sealed class DatabaseStack : Stack
{
    public DatabaseStack(Construct scope, string id, StackProps props, NetworkStack networkStack)
        : base(scope, id, props)
    {
        var cluster = new DatabaseCluster(this, DatabaseConstants.ClusterConstructId, new DatabaseClusterProps
        {
            ClusterIdentifier = DatabaseConstants.ClusterIdentifier,
            Engine = DatabaseClusterEngine.AuroraPostgres(new AuroraPostgresClusterEngineProps
            {
                Version = AuroraPostgresEngineVersion.VER_16_6,
            }),
            Writer = ClusterInstance.ServerlessV2(DatabaseConstants.WriterInstanceIdentifier),
            Vpc = networkStack.Vpc,
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PRIVATE_ISOLATED,
            },
            SecurityGroups = [networkStack.AuroraSg],
            Credentials = Credentials.FromGeneratedSecret(DatabaseConstants.AdminUserName, new CredentialsBaseOptions
            {
                SecretName = DatabaseConstants.SecretName,
            }),
            DefaultDatabaseName = DatabaseConstants.DefaultDatabaseName,
            ServerlessV2MinCapacity = DatabaseConstants.MinAcu,
            ServerlessV2MaxCapacity = DatabaseConstants.MaxAcu,
            Backup = new BackupProps
            {
                Retention = Duration.Days(DatabaseConstants.BackupRetentionDays),
            },
            DeletionProtection = DatabaseConstants.DeletionProtectionEnabled,
            StorageEncrypted = DatabaseConstants.StorageEncrypted,
            RemovalPolicy = RemovalPolicy.DESTROY,
        });

        DbCluster = cluster;
        DbSecret = cluster.Secret ?? throw new InvalidOperationException("The database cluster secret was not generated.");

        _ = new CfnOutput(this, DatabaseConstants.ClusterEndpointOutputId, new CfnOutputProps
        {
            Value = cluster.ClusterEndpoint.Hostname,
        });
        _ = new CfnOutput(this, DatabaseConstants.ClusterPortOutputId, new CfnOutputProps
        {
            Value = cluster.ClusterEndpoint.Port.ToString(CultureInfo.InvariantCulture),
        });
        _ = new CfnOutput(this, DatabaseConstants.SecretArnOutputId, new CfnOutputProps
        {
            Value = DbSecret.SecretArn,
        });
    }

    public DatabaseCluster DbCluster { get; }

    public ISecret DbSecret { get; }
}
