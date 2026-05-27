// <copyright file="ObservabilityStack.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Minimal.Stacks;

using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;
using Serverless.IaC.Minimal.Constants;
using LambdaFunction = Amazon.CDK.AWS.Lambda.IFunction;

internal sealed class ObservabilityStack : Stack
{
    public ObservabilityStack(Construct scope, string id, LambdaFunction sampleFunction, IHttpApi httpApi, IDistribution distribution, ITableV2 sampleTable, IStackProps? props = null)
        : base(scope, id, props)
    {
        this.CreateLambdaAlarms(sampleFunction);
        this.CreateApiAlarm(httpApi);
        this.CreateDynamoAlarm(sampleTable);
        this.CreateCloudFrontAlarm(distribution);
        this.CreateDashboard(sampleFunction, httpApi, distribution, sampleTable);
    }

    private void CreateApiAlarm(IHttpApi httpApi)
    {
        _ = new Alarm(this, "HttpApi5xxAlarm", new AlarmProps
        {
            AlarmName = $"{AppConstants.StackPrefix}-HttpApi-5xx",
            Metric = httpApi.MetricServerError(new MetricOptions { Period = Duration.Minutes(5), Statistic = "Sum" }),
            Threshold = CloudWatchConstants.HttpApi5xxThreshold,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
        });
    }

    private void CreateCloudFrontAlarm(IDistribution distribution)
    {
        _ = new Alarm(this, "CloudFront5xxAlarm", new AlarmProps
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
    }

    private void CreateDashboard(LambdaFunction sampleFunction, IHttpApi httpApi, IDistribution distribution, ITableV2 sampleTable)
    {
        var dashboard = new Dashboard(this, "Dashboard", new DashboardProps { DashboardName = CloudWatchConstants.DashboardName });

        dashboard.AddWidgets(
            new GraphWidget(new GraphWidgetProps { Title = "Lambda Sample", Left = [sampleFunction.MetricErrors(), sampleFunction.MetricThrottles(), sampleFunction.MetricDuration()], Width = 12 }),
            new GraphWidget(new GraphWidgetProps { Title = "HTTP API 5xx", Left = [httpApi.MetricServerError(new MetricOptions { Statistic = "Sum" })], Width = 12 }));

        dashboard.AddWidgets(
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
            }),
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
                Width = 12,
            }));
    }

    private void CreateDynamoAlarm(ITableV2 sampleTable)
    {
        _ = new Alarm(this, "DynamoThrottleAlarm", new AlarmProps
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
    }

    private void CreateLambdaAlarms(LambdaFunction sampleFunction)
    {
        _ = new Alarm(this, "SampleErrorsAlarm", new AlarmProps
        {
            AlarmName = $"{AppConstants.StackPrefix}-Lambda-Sample-Errors",
            Metric = sampleFunction.MetricErrors(new MetricOptions { Period = Duration.Minutes(5) }),
            Threshold = CloudWatchConstants.LambdaErrorsThreshold,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
        });
        _ = new Alarm(this, "SampleDurationAlarm", new AlarmProps
        {
            AlarmName = $"{AppConstants.StackPrefix}-Lambda-Sample-Duration",
            Metric = sampleFunction.MetricDuration(new MetricOptions { Period = Duration.Minutes(5), Statistic = "p99" }),
            Threshold = CloudWatchConstants.LambdaDurationThresholdMs,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
        });
        _ = new Alarm(this, "SampleThrottlesAlarm", new AlarmProps
        {
            AlarmName = $"{AppConstants.StackPrefix}-Lambda-Sample-Throttles",
            Metric = sampleFunction.MetricThrottles(new MetricOptions { Period = Duration.Minutes(1) }),
            Threshold = CloudWatchConstants.LambdaThrottlesThreshold,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
        });
    }
}
