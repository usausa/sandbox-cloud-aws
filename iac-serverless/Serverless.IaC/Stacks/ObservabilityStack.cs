// <copyright file="ObservabilityStack.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Stacks;

using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.CloudWatch.Actions;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Constructs;
using Serverless.IaC.Constants;
using LambdaFunction = Amazon.CDK.AWS.Lambda.IFunction;

internal sealed class ObservabilityStack : Stack
{
    public ObservabilityStack(Construct scope, string id, LambdaFunction sampleFunction, LambdaFunction sampleVpcFunction, IHttpApi httpApi, IDistribution distribution, ITableV2 sampleTable, IStackProps? props = null)
        : base(scope, id, props)
    {
        var alarmTopic = new Topic(this, "AlarmTopic", new TopicProps { TopicName = ObservabilityConstants.AlarmTopicName });
        alarmTopic.AddSubscription(new EmailSubscription(ObservabilityConstants.AlarmEmail));

        var alarmAction = new SnsAction(alarmTopic);
        this.CreateLambdaAlarms(sampleFunction, "Sample", alarmAction);
        this.CreateLambdaAlarms(sampleVpcFunction, "SampleVpc", alarmAction);
        this.CreateApiAlarm(httpApi, alarmAction);
        this.CreateDynamoAlarm(sampleTable, alarmAction);
        this.CreateCloudFrontAlarm(distribution, alarmAction);
        this.CreateDashboard(sampleFunction, sampleVpcFunction, httpApi, distribution, sampleTable);
    }

    private void CreateApiAlarm(IHttpApi httpApi, IAlarmAction alarmAction)
    {
        var alarm = new Alarm(this, "HttpApi5xxAlarm", new AlarmProps
        {
            AlarmName = $"{AppConstants.StackPrefix}-HttpApi-5xx",
            Metric = httpApi.MetricServerError(new MetricOptions { Period = Duration.Minutes(5), Statistic = "Sum" }),
            Threshold = CloudWatchConstants.HttpApi5xxThreshold,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
        });
        alarm.AddAlarmAction(alarmAction);
    }

    private void CreateCloudFrontAlarm(IDistribution distribution, IAlarmAction alarmAction)
    {
        var alarm = new Alarm(this, "CloudFront5xxAlarm", new AlarmProps
        {
            AlarmName = $"{AppConstants.StackPrefix}-CloudFront-5xxErrorRate",
            Metric = new Metric(new MetricProps
            {
                Namespace = "AWS/CloudFront",
                MetricName = CloudWatchConstants.CloudFrontErrorRateMetricName,
                DimensionsMap = new Dictionary<string, string>
                {
                    ["DistributionId"] = distribution.DistributionId,
                    ["Region"] = CloudWatchConstants.GlobalRegion,
                },
                Period = Duration.Minutes(5),
                Statistic = "Average",
            }),
            Threshold = CloudWatchConstants.CloudFront5xxThreshold,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
        });
        alarm.AddAlarmAction(alarmAction);
    }

    private void CreateDashboard(LambdaFunction sampleFunction, LambdaFunction sampleVpcFunction, IHttpApi httpApi, IDistribution distribution, ITableV2 sampleTable)
    {
        var dashboard = new Dashboard(this, "Dashboard", new DashboardProps { DashboardName = CloudWatchConstants.DashboardName });

        dashboard.AddWidgets(
            new GraphWidget(new GraphWidgetProps { Title = "Lambda Sample", Left = [sampleFunction.MetricErrors(), sampleFunction.MetricThrottles(), sampleFunction.MetricDuration()], Width = 12 }),
            new GraphWidget(new GraphWidgetProps { Title = "Lambda SampleVpc", Left = [sampleVpcFunction.MetricErrors(), sampleVpcFunction.MetricThrottles(), sampleVpcFunction.MetricDuration()], Width = 12 }));

        dashboard.AddWidgets(
            new GraphWidget(new GraphWidgetProps { Title = "HTTP API 5xx", Left = [httpApi.MetricServerError(new MetricOptions { Statistic = "Sum" })], Width = 12 }),
            new GraphWidget(new GraphWidgetProps
            {
                Title = "DynamoDB ThrottledRequests",
                Left =
                [
                    new Metric(new MetricProps
                    {
                        Namespace = CloudWatchConstants.DynamoDbNamespace,
                        MetricName = CloudWatchConstants.DynamoThrottledRequestsMetricName,
                        DimensionsMap = new Dictionary<string, string> { ["TableName"] = sampleTable.TableName },
                        Statistic = "Sum",
                    }),
                ],
                Width = 12,
            }));

        dashboard.AddWidgets(
            new GraphWidget(new GraphWidgetProps
            {
                Title = "CloudFront 5xx Error Rate",
                Left =
                [
                    new Metric(new MetricProps
                    {
                        Namespace = "AWS/CloudFront",
                        MetricName = CloudWatchConstants.CloudFrontErrorRateMetricName,
                        DimensionsMap = new Dictionary<string, string>
                        {
                            ["DistributionId"] = distribution.DistributionId,
                            ["Region"] = CloudWatchConstants.GlobalRegion,
                        },
                        Statistic = "Average",
                    }),
                ],
                Width = 24,
            }));
    }

    private void CreateDynamoAlarm(ITableV2 sampleTable, IAlarmAction alarmAction)
    {
        var alarm = new Alarm(this, "DynamoThrottleAlarm", new AlarmProps
        {
            AlarmName = $"{AppConstants.StackPrefix}-DynamoDb-ThrottledRequests",
            Metric = new Metric(new MetricProps
            {
                Namespace = CloudWatchConstants.DynamoDbNamespace,
                MetricName = CloudWatchConstants.DynamoThrottledRequestsMetricName,
                DimensionsMap = new Dictionary<string, string> { ["TableName"] = sampleTable.TableName },
                Period = Duration.Minutes(5),
                Statistic = "Sum",
            }),
            Threshold = CloudWatchConstants.DynamoThrottleThreshold,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
        });
        alarm.AddAlarmAction(alarmAction);
    }

    private void CreateLambdaAlarms(LambdaFunction function, string nameSuffix, IAlarmAction alarmAction)
    {
        var errorsAlarm = new Alarm(this, $"{nameSuffix}ErrorsAlarm", new AlarmProps
        {
            AlarmName = $"{AppConstants.StackPrefix}-Lambda-{nameSuffix}-Errors",
            Metric = function.MetricErrors(new MetricOptions { Period = Duration.Minutes(5) }),
            Threshold = CloudWatchConstants.LambdaErrorsThreshold,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
        });
        errorsAlarm.AddAlarmAction(alarmAction);

        var durationAlarm = new Alarm(this, $"{nameSuffix}DurationAlarm", new AlarmProps
        {
            AlarmName = $"{AppConstants.StackPrefix}-Lambda-{nameSuffix}-Duration",
            Metric = function.MetricDuration(new MetricOptions { Period = Duration.Minutes(5), Statistic = "p99" }),
            Threshold = CloudWatchConstants.LambdaDurationThresholdMs,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
        });
        durationAlarm.AddAlarmAction(alarmAction);

        var throttlesAlarm = new Alarm(this, $"{nameSuffix}ThrottlesAlarm", new AlarmProps
        {
            AlarmName = $"{AppConstants.StackPrefix}-Lambda-{nameSuffix}-Throttles",
            Metric = function.MetricThrottles(new MetricOptions { Period = Duration.Minutes(1) }),
            Threshold = CloudWatchConstants.LambdaThrottlesThreshold,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
        });
        throttlesAlarm.AddAlarmAction(alarmAction);
    }
}
