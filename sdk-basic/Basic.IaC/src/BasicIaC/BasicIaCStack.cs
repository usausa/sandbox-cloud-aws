namespace BasicIaC;

using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Constructs;

internal sealed class BasicIaCStack : Stack
{
    internal BasicIaCStack(Construct scope, string id, IStackProps? props = null)
        : base(scope, id, props)
    {
        var dbUser = System.Environment.GetEnvironmentVariable("DB_USERNAME") ?? "dbadmin";
        var dbPassword = System.Environment.GetEnvironmentVariable("DB_PASSWORD") ?? throw new InvalidOperationException("DB_PASSWORD environment variable is required.");
        var dbName = System.Environment.GetEnvironmentVariable("DB_NAME") ?? "appdb";

        // VPC: public subnets only, no NAT gateway
        var vpc = new Vpc(this, "AuroraVpc", new VpcProps
        {
            MaxAzs = 2,
            SubnetConfiguration =
            [
                new SubnetConfiguration
                {
                    Name = "Public",
                    SubnetType = SubnetType.PUBLIC,
                    CidrMask = 24,
                },
            ],
            NatGateways = 0,
        });

        // Security group: allow PostgreSQL port from anywhere
        var dbSecurityGroup = new SecurityGroup(this, "DbSecurityGroup", new SecurityGroupProps
        {
            Vpc = vpc,
            Description = "Allow public access to Aurora Serverless PostgreSQL",
            AllowAllOutbound = true,
        });
        dbSecurityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(5432), "PostgreSQL from anywhere");

        // Credentials from environment variables (no Secrets Manager)
        var dbCredentials = Credentials.FromPassword(dbUser, SecretValue.UnsafePlainText(dbPassword));

        // Aurora Serverless v2 cluster (PostgreSQL compatible)
        var cluster = new DatabaseCluster(this, "AuroraCluster", new DatabaseClusterProps
        {
            Engine = DatabaseClusterEngine.AuroraPostgres(new AuroraPostgresClusterEngineProps
            {
                Version = AuroraPostgresEngineVersion.VER_16_6,
            }),
            Credentials = dbCredentials,
            ServerlessV2MinCapacity = 0.5,
            ServerlessV2MaxCapacity = 1,
            Writer = ClusterInstance.ServerlessV2("writer"),
            Vpc = vpc,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
            SecurityGroups = [dbSecurityGroup],
            DefaultDatabaseName = dbName,
            StorageEncrypted = false,
            EnablePerformanceInsights = false,
            CloudwatchLogsRetention = Amazon.CDK.AWS.Logs.RetentionDays.ONE_WEEK,
            DeletionProtection = false,
            RemovalPolicy = RemovalPolicy.DESTROY,
        });

        // Enable public accessibility on the writer instance via escape hatch
        var cfnInstance = (CfnDBInstance)cluster.Node.FindChild("writer").Node.DefaultChild!;
        cfnInstance.PubliclyAccessible = true;

        _ = new CfnOutput(this, "ClusterEndpoint", new CfnOutputProps
        {
            Value = cluster.ClusterEndpoint.Hostname,
            Description = "Aurora Serverless PostgreSQL cluster endpoint",
        });
    }
}
