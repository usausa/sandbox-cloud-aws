using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Ecr.Assets;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.ServiceDiscovery;

using Constructs;

namespace TenantApi.IaC.Constructs;

internal sealed class ContainerConstructProps
{
    public required Vpc Vpc { get; init; }

    public required SecurityGroup EcsTaskSecurityGroup { get; init; }
}

/// <summary>
/// ECS Cluster, Task / Service, Cloud Map service registration and the
/// CloudWatch Log Group. Container images are built and pushed by CDK from
/// the local Dockerfile.
/// </summary>
internal sealed class ContainerConstruct : Construct
{
    public Cluster Cluster { get; }

    public FargateService Service { get; }

    public Amazon.CDK.AWS.ServiceDiscovery.IService CloudMapService { get; }

    public ContainerConstruct(Construct scope, string id, ContainerConstructProps props)
        : base(scope, id)
    {
        var namespaceConstruct = new PrivateDnsNamespace(this, "Namespace", new PrivateDnsNamespaceProps
        {
            Name = Constants.CloudMapNamespace,
            Vpc = props.Vpc,
        });

        Cluster = new Cluster(this, "Cluster", new ClusterProps
        {
            ClusterName = Constants.EcsClusterName,
            Vpc = props.Vpc,
            EnableFargateCapacityProviders = true,
        });

        var logGroup = new LogGroup(this, "LogGroup", new LogGroupProps
        {
            LogGroupName = Constants.LogGroupName,
            Retention = (RetentionDays)Constants.LogRetentionDays,
            RemovalPolicy = RemovalPolicy.DESTROY,
        });

        var taskDefinition = new FargateTaskDefinition(this, "TaskDef", new FargateTaskDefinitionProps
        {
            Cpu = Constants.ContainerCpu,
            MemoryLimitMiB = Constants.ContainerMemoryMiB,
        });

        var image = ContainerImage.FromAsset(Constants.DockerBuildContext, new AssetImageProps
        {
            File = Constants.DockerfilePath,
            Platform = Platform_.LINUX_AMD64,
        });

        _ = taskDefinition.AddContainer("App", new ContainerDefinitionOptions
        {
            ContainerName = Constants.ContainerName,
            Image = image,
            Logging = LogDrivers.AwsLogs(new AwsLogDriverProps
            {
                LogGroup = logGroup,
                StreamPrefix = "app",
            }),
            Environment = new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["ASPNETCORE_URLS"] = $"http://+:{Constants.ContainerPort}",
            },
            PortMappings =
            [
                new PortMapping { ContainerPort = Constants.ContainerPort },
            ],
        });

        Service = new FargateService(this, "Service", new FargateServiceProps
        {
            ServiceName = Constants.EcsServiceName,
            Cluster = Cluster,
            TaskDefinition = taskDefinition,
            DesiredCount = Constants.DesiredCount,
            AssignPublicIp = false,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED },
            SecurityGroups = [props.EcsTaskSecurityGroup],
            MinHealthyPercent = 0,
            MaxHealthyPercent = 100,
            CapacityProviderStrategies =
            [
                new CapacityProviderStrategy
                {
                    CapacityProvider = "FARGATE_SPOT",
                    Weight = 1,
                },
            ],
            CloudMapOptions = new CloudMapOptions
            {
                CloudMapNamespace = namespaceConstruct,
                Name = Constants.CloudMapServiceName,
                DnsRecordType = DnsRecordType.A,
                DnsTtl = Duration.Seconds(10),
            },
        });

        CloudMapService = (Amazon.CDK.AWS.ServiceDiscovery.IService)Service.CloudMapService!;
    }
}
