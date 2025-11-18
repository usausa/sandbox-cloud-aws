using Amazon.CDK;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.Scheduler;

using Constructs;

namespace TenantApi.IaC.Constructs;

internal sealed class SchedulerConstructProps
{
    public required Cluster EcsCluster { get; init; }

    public required FargateService EcsService { get; init; }

    public required DatabaseCluster DbCluster { get; init; }
}

/// <summary>
/// EventBridge Scheduler entries that start / stop ECS service and Aurora
/// cluster on a weekday business-hours schedule via Lambda functions.
/// </summary>
internal sealed class SchedulerConstruct : Construct
{
    public SchedulerConstruct(Construct scope, string id, SchedulerConstructProps props)
        : base(scope, id)
    {
        var environment = new Dictionary<string, string>
        {
            ["ECS_CLUSTER"] = props.EcsCluster.ClusterName,
            ["ECS_SERVICE"] = props.EcsService.ServiceName,
            ["DB_CLUSTER_ID"] = props.DbCluster.ClusterIdentifier,
        };

        var startLambda = new Function(this, "StartLambda", new FunctionProps
        {
            Runtime = Runtime.NODEJS_22_X,
            Handler = "index.handler",
            Code = Code.FromAsset(Constants.StartLambdaPath, new Amazon.CDK.AWS.S3.Assets.AssetOptions
            {
                Bundling = new BundlingOptions
                {
                    Image = Runtime.NODEJS_22_X.BundlingImage,
                    Command = ["bash", "-c", "npm ci && cp -r . /asset-output"],
                },
            }),
            Timeout = Duration.Seconds(60),
            MemorySize = 128,
            Environment = environment,
        });

        var stopLambda = new Function(this, "StopLambda", new FunctionProps
        {
            Runtime = Runtime.NODEJS_22_X,
            Handler = "index.handler",
            Code = Code.FromAsset(Constants.StopLambdaPath, new Amazon.CDK.AWS.S3.Assets.AssetOptions
            {
                Bundling = new BundlingOptions
                {
                    Image = Runtime.NODEJS_22_X.BundlingImage,
                    Command = ["bash", "-c", "npm ci && cp -r . /asset-output"],
                },
            }),
            Timeout = Duration.Seconds(60),
            MemorySize = 128,
            Environment = environment,
        });

        // ECS UpdateService permissions (limited to this service's ARN)
        var ecsPolicy = new PolicyStatement(new PolicyStatementProps
        {
            Actions = ["ecs:UpdateService", "ecs:DescribeServices"],
            Resources = [props.EcsService.ServiceArn],
        });

        // Aurora Start/Stop (ARN scoping for Start/StopDBCluster).
        var stack = Stack.Of(this);
        var dbClusterArn = $"arn:{stack.Partition}:rds:{stack.Region}:{stack.Account}:cluster:{props.DbCluster.ClusterIdentifier}";
        var rdsPolicy = new PolicyStatement(new PolicyStatementProps
        {
            Actions = ["rds:StartDBCluster", "rds:StopDBCluster", "rds:DescribeDBClusters"],
            Resources = [dbClusterArn],
        });

        startLambda.AddToRolePolicy(ecsPolicy);
        startLambda.AddToRolePolicy(rdsPolicy);
        stopLambda.AddToRolePolicy(ecsPolicy);
        stopLambda.AddToRolePolicy(rdsPolicy);

        var schedulerRole = new Role(this, "SchedulerRole", new RoleProps
        {
            AssumedBy = new ServicePrincipal("scheduler.amazonaws.com"),
        });
        startLambda.GrantInvoke(schedulerRole);
        stopLambda.GrantInvoke(schedulerRole);

        // Aurora 起動
        _ = new CfnSchedule(this, "AuroraStartSchedule", new CfnScheduleProps
        {
            FlexibleTimeWindow = new CfnSchedule.FlexibleTimeWindowProperty { Mode = "OFF" },
            ScheduleExpression = Constants.ScheduleAuroraStart,
            ScheduleExpressionTimezone = Constants.ScheduleTimeZone,
            Target = new CfnSchedule.TargetProperty
            {
                Arn = startLambda.FunctionArn,
                RoleArn = schedulerRole.RoleArn,
                Input = "{\"action\":\"start-db\"}",
            },
        });

        // ECS 起動
        _ = new CfnSchedule(this, "EcsStartSchedule", new CfnScheduleProps
        {
            FlexibleTimeWindow = new CfnSchedule.FlexibleTimeWindowProperty { Mode = "OFF" },
            ScheduleExpression = Constants.ScheduleEcsStart,
            ScheduleExpressionTimezone = Constants.ScheduleTimeZone,
            Target = new CfnSchedule.TargetProperty
            {
                Arn = startLambda.FunctionArn,
                RoleArn = schedulerRole.RoleArn,
                Input = "{\"action\":\"start-ecs\"}",
            },
        });

        // ECS 停止
        _ = new CfnSchedule(this, "EcsStopSchedule", new CfnScheduleProps
        {
            FlexibleTimeWindow = new CfnSchedule.FlexibleTimeWindowProperty { Mode = "OFF" },
            ScheduleExpression = Constants.ScheduleEcsStop,
            ScheduleExpressionTimezone = Constants.ScheduleTimeZone,
            Target = new CfnSchedule.TargetProperty
            {
                Arn = stopLambda.FunctionArn,
                RoleArn = schedulerRole.RoleArn,
                Input = "{\"action\":\"stop-ecs\"}",
            },
        });

        // Aurora 停止
        _ = new CfnSchedule(this, "AuroraStopSchedule", new CfnScheduleProps
        {
            FlexibleTimeWindow = new CfnSchedule.FlexibleTimeWindowProperty { Mode = "OFF" },
            ScheduleExpression = Constants.ScheduleAuroraStop,
            ScheduleExpressionTimezone = Constants.ScheduleTimeZone,
            Target = new CfnSchedule.TargetProperty
            {
                Arn = stopLambda.FunctionArn,
                RoleArn = schedulerRole.RoleArn,
                Input = "{\"action\":\"stop-db\"}",
            },
        });
    }
}
