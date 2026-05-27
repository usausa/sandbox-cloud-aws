namespace Tenant.IaC.Minimal.Stacks;

using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;
using Tenant.IaC.Minimal.Constants;

internal sealed class MinimalStack : Stack
{
    internal MinimalStack(Construct scope, string id, IStackProps? props = null)
        : base(scope, id, props)
    {
        var vpc = new Vpc(this, "Vpc", new VpcProps
        {
            IpAddresses = IpAddresses.Cidr(MinimalConstants.VpcCidr),
            MaxAzs = MinimalConstants.MaxAzs,
            NatGateways = 0,
            SubnetConfiguration =
            [
                new SubnetConfiguration
                {
                    Name = "Public",
                    CidrMask = 24,
                    SubnetType = SubnetType.PUBLIC,
                },
            ],
        });

        var albSg = new SecurityGroup(this, "AlbSg", new SecurityGroupProps
        {
            Vpc = vpc,
            AllowAllOutbound = true,
            Description = "Security group for the internet-facing ALB.",
        });
        albSg.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(MinimalConstants.AlbPort), "Allow HTTP from the internet.");

        var ecsSg = new SecurityGroup(this, "EcsSg", new SecurityGroupProps
        {
            Vpc = vpc,
            AllowAllOutbound = true,
            Description = "Security group for the Fargate service.",
        });
        ecsSg.AddIngressRule(albSg, Port.Tcp(MinimalConstants.ContainerPort), "Allow traffic from the ALB.");

        var repository = new Repository(this, "Repository", new RepositoryProps
        {
            RepositoryName = MinimalConstants.EcrRepositoryName,
            ImageScanOnPush = true,
            ImageTagMutability = TagMutability.MUTABLE,
            LifecycleRules =
            [
                new LifecycleRule
                {
                    MaxImageCount = MinimalConstants.EcrImageRetentionCount,
                },
            ],
            RemovalPolicy = RemovalPolicy.RETAIN,
        });

        var cluster = new Cluster(this, "Cluster", new ClusterProps
        {
            Vpc = vpc,
            ClusterName = MinimalConstants.ClusterName,
        });

        var logGroup = new LogGroup(this, "LogGroup", new LogGroupProps
        {
            LogGroupName = MinimalConstants.LogGroupName,
            Retention = (RetentionDays)MinimalConstants.LogRetentionDays,
            RemovalPolicy = RemovalPolicy.DESTROY,
        });

        var taskDefinition = new FargateTaskDefinition(this, "TaskDefinition", new FargateTaskDefinitionProps
        {
            Cpu = MinimalConstants.CpuUnits,
            MemoryLimitMiB = MinimalConstants.MemoryMiB,
            Family = MinimalConstants.TaskFamily,
        });

        var container = taskDefinition.AddContainer("Container", new ContainerDefinitionOptions
        {
            ContainerName = MinimalConstants.ContainerName,
            Image = ContainerImage.FromEcrRepository(repository, MinimalConstants.ImageTag),
            Logging = LogDriver.AwsLogs(new AwsLogDriverProps
            {
                LogGroup = logGroup,
                StreamPrefix = MinimalConstants.ContainerName,
            }),
        });
        container.AddPortMappings(new PortMapping
        {
            ContainerPort = MinimalConstants.ContainerPort,
        });

        var service = new FargateService(this, "Service", new FargateServiceProps
        {
            Cluster = cluster,
            TaskDefinition = taskDefinition,
            ServiceName = MinimalConstants.ServiceName,
            DesiredCount = MinimalConstants.DesiredCount,
            AssignPublicIp = true,
            SecurityGroups = [ecsSg],
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PUBLIC,
            },
            CapacityProviderStrategies =
            [
                new CapacityProviderStrategy
                {
                    CapacityProvider = "FARGATE_SPOT",
                    Weight = 1,
                },
            ],
        });

        var loadBalancer = new ApplicationLoadBalancer(this, "Alb", new ApplicationLoadBalancerProps
        {
            Vpc = vpc,
            LoadBalancerName = MinimalConstants.AlbName,
            InternetFacing = true,
            SecurityGroup = albSg,
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PUBLIC,
            },
        });

        var listener = loadBalancer.AddListener("HttpListener", new BaseApplicationListenerProps
        {
            Port = MinimalConstants.AlbPort,
            Open = false,
        });

        _ = listener.AddTargets("EcsTargets", new AddApplicationTargetsProps
        {
            Port = MinimalConstants.ContainerPort,
            Targets = [service],
            HealthCheck = new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
            {
                Path = MinimalConstants.HealthCheckPath,
                Interval = Duration.Seconds(30),
                Timeout = Duration.Seconds(5),
                HealthyThresholdCount = 2,
                UnhealthyThresholdCount = 3,
            },
        });

        var api = new HttpApi(this, "HttpApi", new HttpApiProps
        {
            ApiName = MinimalConstants.ApiName,
            CorsPreflight = new CorsPreflightOptions
            {
                AllowOrigins = ["*"],
                AllowMethods = [CorsHttpMethod.ANY],
                AllowHeaders = ["Authorization", "Content-Type"],
            },
        });

        var integration = new HttpUrlIntegration("AlbIntegration", $"http://{loadBalancer.LoadBalancerDnsName}/{{proxy}}");
        api.AddRoutes(new AddRoutesOptions
        {
            Path = "/{proxy+}",
            Methods = [Amazon.CDK.AWS.Apigatewayv2.HttpMethod.ANY],
            Integration = integration,
        });

        _ = new CfnOutput(this, "EcrRepositoryUri", new CfnOutputProps
        {
            Value = repository.RepositoryUri,
        });

        _ = new CfnOutput(this, "AlbDnsName", new CfnOutputProps
        {
            Value = loadBalancer.LoadBalancerDnsName,
        });

        _ = new CfnOutput(this, "ApiEndpoint", new CfnOutputProps
        {
            Value = api.ApiEndpoint,
        });
    }
}
