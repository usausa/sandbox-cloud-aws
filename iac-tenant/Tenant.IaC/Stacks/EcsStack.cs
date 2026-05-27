namespace Tenant.IaC.Stacks;

using System;
using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.Logs;
using Constructs;
using Tenant.IaC.Constants;
using EcsSecret = Amazon.CDK.AWS.ECS.Secret;

internal sealed class EcsStack : Stack
{
    public EcsStack(Construct scope, string id, StackProps props, NetworkStack networkStack, DatabaseStack databaseStack)
        : base(scope, id, props)
    {
        var ecrRepository = new Repository(this, EcsConstants.RepositoryConstructId, new RepositoryProps
        {
            RepositoryName = EcsConstants.EcrRepositoryName,
            ImageTagMutability = TagMutability.MUTABLE,
            ImageScanOnPush = true,
            RemovalPolicy = RemovalPolicy.RETAIN,
            LifecycleRules =
            [
                new LifecycleRule
                {
                    MaxImageCount = EcsConstants.EcrImageRetentionCount,
                },
            ],
        });

        var cluster = new Cluster(this, EcsConstants.ClusterConstructId, new ClusterProps
        {
            Vpc = networkStack.Vpc,
            ClusterName = EcsConstants.ClusterName,
        });

        var logGroup = new LogGroup(this, EcsConstants.LogGroupConstructId, new LogGroupProps
        {
            LogGroupName = EcsConstants.LogGroupName,
            Retention = ResolveLogRetentionDays(),
        });

        var taskDefinition = new FargateTaskDefinition(this, EcsConstants.TaskDefinitionConstructId, new FargateTaskDefinitionProps
        {
            Cpu = EcsConstants.CpuUnits,
            MemoryLimitMiB = EcsConstants.MemoryMiB,
            Family = EcsConstants.TaskFamily,
        });

        _ = taskDefinition.AddContainer(EcsConstants.ContainerName, new ContainerDefinitionOptions
        {
            ContainerName = EcsConstants.ContainerName,
            Image = ContainerImage.FromEcrRepository(ecrRepository, EcsConstants.ImageTag),
            Logging = LogDrivers.AwsLogs(new AwsLogDriverProps
            {
                LogGroup = logGroup,
                StreamPrefix = EcsConstants.LogStreamPrefix,
            }),
            Environment = new Dictionary<string, string>
            {
                [EcsConstants.DbHostEnvironmentName] = databaseStack.DbCluster.ClusterEndpoint.Hostname,
                [EcsConstants.DbNameEnvironmentName] = DatabaseConstants.DefaultDatabaseName,
                [EcsConstants.AwsRegionEnvironmentName] = Region,
            },
            Secrets = new Dictionary<string, EcsSecret>
            {
                [EcsConstants.DbUsernameSecretName] = EcsSecret.FromSecretsManager(databaseStack.DbSecret, DatabaseConstants.SecretUsernameField),
                [EcsConstants.DbPasswordSecretName] = EcsSecret.FromSecretsManager(databaseStack.DbSecret, DatabaseConstants.SecretPasswordField),
            },
            PortMappings =
            [
                new PortMapping
                {
                    ContainerPort = NetworkConstants.ContainerPort,
                },
            ],
        });

        var service = new FargateService(this, EcsConstants.ServiceConstructId, new FargateServiceProps
        {
            Cluster = cluster,
            ServiceName = EcsConstants.ServiceName,
            TaskDefinition = taskDefinition,
            DesiredCount = EcsConstants.DesiredCount,
            SecurityGroups = [networkStack.EcsSg],
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PUBLIC,
            },
            AssignPublicIp = true,
            CapacityProviderStrategies =
            [
                new CapacityProviderStrategy
                {
                    CapacityProvider = EcsConstants.FargateSpotCapacityProvider,
                    Weight = EcsConstants.FargateSpotWeight,
                },
            ],
        });

        var loadBalancer = new ApplicationLoadBalancer(this, EcsConstants.AlbConstructId, new ApplicationLoadBalancerProps
        {
            Vpc = networkStack.Vpc,
            LoadBalancerName = EcsConstants.AlbName,
            InternetFacing = true,
            SecurityGroup = networkStack.AlbSg,
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PUBLIC,
            },
        });

        var listener = loadBalancer.AddListener(EcsConstants.ListenerConstructId, new BaseApplicationListenerProps
        {
            Port = NetworkConstants.AlbPort,
            Protocol = ApplicationProtocol.HTTP,
            Open = false,
        });

        _ = listener.AddTargets(EcsConstants.TargetGroupConstructId, new AddApplicationTargetsProps
        {
            Port = NetworkConstants.ContainerPort,
            Targets =
            [
                service.LoadBalancerTarget(new LoadBalancerTargetOptions
                {
                    ContainerName = EcsConstants.ContainerName,
                    ContainerPort = NetworkConstants.ContainerPort,
                }),
            ],
            HealthCheck = new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
            {
                Path = EcsConstants.HealthCheckPath,
                HealthyHttpCodes = EcsConstants.HealthCheckSuccessCodes,
            },
        });

        AlbDnsName = loadBalancer.LoadBalancerDnsName;

        _ = new CfnOutput(this, EcsConstants.EcrRepositoryUriOutputId, new CfnOutputProps
        {
            Value = ecrRepository.RepositoryUri,
        });
        _ = new CfnOutput(this, EcsConstants.ClusterNameOutputId, new CfnOutputProps
        {
            Value = cluster.ClusterName,
        });
        _ = new CfnOutput(this, EcsConstants.ServiceNameOutputId, new CfnOutputProps
        {
            Value = service.ServiceName,
        });
        _ = new CfnOutput(this, EcsConstants.AlbDnsNameOutputId, new CfnOutputProps
        {
            Value = AlbDnsName,
        });
    }

    public string AlbDnsName { get; }

    private static RetentionDays ResolveLogRetentionDays() => EcsConstants.LogRetentionDays switch
    {
        7 => RetentionDays.ONE_WEEK,
        14 => RetentionDays.TWO_WEEKS,
        30 => RetentionDays.ONE_MONTH,
        _ => throw new InvalidOperationException($"Unsupported log retention days: {EcsConstants.LogRetentionDays}"),
    };
}
