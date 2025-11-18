namespace TenantApi.IaC;

using Amazon.CDK;

using TenantApi.IaC.Constructs;

using Construct = global::Constructs.Construct;

internal sealed class MainStack : Stack
{
    public MainStack(Construct scope, string id, IStackProps props)
        : base(scope, id, props)
    {
        var network = new NetworkConstruct(this, "Network");

        var database = new DatabaseConstruct(this, "Database", new DatabaseConstructProps
        {
            Vpc = network.Vpc,
            AuroraSecurityGroup = network.AuroraSecurityGroup,
        });

        var auth = new AuthConstruct(this, "Auth");

        var container = new ContainerConstruct(this, "Container", new ContainerConstructProps
        {
            Vpc = network.Vpc,
            EcsTaskSecurityGroup = network.EcsTaskSecurityGroup,
        });

        var api = new ApiConstruct(this, "Api", new ApiConstructProps
        {
            Vpc = network.Vpc,
            UserPool = auth.UserPool,
            UserPoolClient = auth.UserPoolClient,
            CloudMapService = container.CloudMapService,
            VpcLinkSecurityGroup = network.VpcLinkSecurityGroup,
        });

        _ = new SchedulerConstruct(this, "Scheduler", new SchedulerConstructProps
        {
            EcsCluster = container.Cluster,
            EcsService = container.Service,
            DbCluster = database.Cluster,
        });

        // Outputs
        _ = new CfnOutput(this, "HttpApiUrl", new CfnOutputProps { Value = api.HttpApiUrl });
        _ = new CfnOutput(this, "UserPoolId", new CfnOutputProps { Value = auth.UserPool.UserPoolId });
        _ = new CfnOutput(this, "UserPoolClientId", new CfnOutputProps { Value = auth.UserPoolClient.UserPoolClientId });
        _ = new CfnOutput(this, "AuroraEndpoint", new CfnOutputProps { Value = database.Cluster.ClusterEndpoint.Hostname });
        _ = new CfnOutput(this, "AuroraSecretArn", new CfnOutputProps { Value = database.SecretArn });
    }
}
