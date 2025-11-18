using System.Collections.Generic;

using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.CloudWatch.Actions;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SNS.Subscriptions;

using Constructs;

using ILambdaFunction = Amazon.CDK.AWS.Lambda.IFunction;

using Serverless.IaC.Configuration;

namespace Serverless.IaC.Stacks;

internal sealed class MonitoringStackProps : StackProps
{
    public required IReadOnlyList<ILambdaFunction> ApiFunctions { get; init; }

    public required HttpApi HttpApi { get; init; }

    public required IReadOnlyList<ITable> Tables { get; init; }

    public required IDistribution Distribution { get; init; }
}

/// <summary>
/// CloudWatch Alarms / Dashboard / SNS 通知。
/// 閾値は <see cref="ProjectConstants"/> に集約し、運用開始後にチューニングする。
/// </summary>
internal sealed class MonitoringStack : Stack
{
    public MonitoringStack(Construct scope, string id, MonitoringStackProps props)
        : base(scope, id, props)
    {
        var alarmTopic = new Topic(this, "AlarmTopic", new TopicProps
        {
            TopicName = $"{ProjectConstants.ResourceSuffix}-alarms",
            DisplayName = $"{ProjectConstants.ResourceSuffix} Alarms",
        });
        alarmTopic.AddSubscription(new EmailSubscription(ProjectConstants.OperatorEmail));

        var alarmAction = new SnsAction(alarmTopic);

        var dashboardWidgets = new List<IWidget>();

        foreach (var fn in props.ApiFunctions)
        {
            CreateAlarm(
                $"{fn.Node.Id}ErrorsAlarm",
                fn.MetricErrors(new MetricOptions { Period = Duration.Minutes(5), Statistic = "Sum" }),
                ProjectConstants.LambdaErrorAlarmThreshold,
                alarmAction);

            CreateAlarm(
                $"{fn.Node.Id}ThrottlesAlarm",
                fn.MetricThrottles(new MetricOptions { Period = Duration.Minutes(1), Statistic = "Sum" }),
                ProjectConstants.LambdaThrottleAlarmThreshold,
                alarmAction);

            var durationThresholdMs = ProjectConstants.LambdaTimeoutSeconds * 1000 * ProjectConstants.LambdaDurationAlarmRatio;
            CreateAlarm(
                $"{fn.Node.Id}DurationAlarm",
                fn.MetricDuration(new MetricOptions { Period = Duration.Minutes(5), Statistic = "p99" }),
                durationThresholdMs,
                alarmAction);

            dashboardWidgets.Add(new GraphWidget(new GraphWidgetProps
            {
                Title = $"Lambda: {fn.FunctionName}",
                Left = [fn.MetricInvocations(), fn.MetricErrors(), fn.MetricThrottles()],
                Right = [fn.MetricDuration(new MetricOptions { Statistic = "p50" }), fn.MetricDuration(new MetricOptions { Statistic = "p99" })],
                Width = 12,
            }));
        }

        foreach (var table in props.Tables)
        {
            CreateAlarm(
                $"{table.Node.Id}ThrottleAlarm",
                new Metric(new MetricProps
                {
                    Namespace = "AWS/DynamoDB",
                    MetricName = "ThrottledRequests",
                    DimensionsMap = new Dictionary<string, string> { ["TableName"] = table.TableName },
                    Statistic = "Sum",
                    Period = Duration.Minutes(5),
                }),
                ProjectConstants.DynamoDbThrottleAlarmThreshold,
                alarmAction);
        }

        // CloudFront 5xx error rate (us-east-1 メトリクスだが Alarm 自身はリージョナル)
        var cf5xx = new Metric(new MetricProps
        {
            Namespace = "AWS/CloudFront",
            MetricName = "5xxErrorRate",
            DimensionsMap = new Dictionary<string, string>
            {
                ["DistributionId"] = props.Distribution.DistributionId,
                ["Region"] = "Global",
            },
            Statistic = "Average",
            Period = Duration.Minutes(5),
            Region = "us-east-1",
        });
        CreateAlarm("CloudFront5xxAlarm", cf5xx, ProjectConstants.CloudFront5xxAlarmRatePercent, alarmAction);

        var dashboard = new Dashboard(this, "Dashboard", new DashboardProps
        {
            DashboardName = $"{ProjectConstants.ResourceSuffix}-dashboard",
        });
        dashboard.AddWidgets([.. dashboardWidgets]);

        AlarmTopic = alarmTopic;
    }

    public ITopic AlarmTopic { get; }

    private void CreateAlarm(string id, IMetric metric, double threshold, SnsAction action)
    {
        var alarm = new Alarm(this, id, new AlarmProps
        {
            Metric = metric,
            Threshold = threshold,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
            TreatMissingData = TreatMissingData.NOT_BREACHING,
        });
        alarm.AddAlarmAction(action);
    }
}
