// <copyright file="ApiStack.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Stacks;

using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AwsApigatewayv2Authorizers;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;
using Serverless.IaC.Constants;
using ApiGatewayAccessLogDestination = Amazon.CDK.AWS.Apigatewayv2.IAccessLogDestination;
using ApiGatewayLogDestination = Amazon.CDK.AWS.Apigatewayv2.LogGroupLogDestination;
using HttpApiMethod = Amazon.CDK.AWS.Apigatewayv2.HttpMethod;

internal sealed class ApiStack : Stack
{
    public ApiStack(Construct scope, string id, IFunction sampleFunction, IStackProps? props = null)
        : base(scope, id, props)
    {
        var accessLogGroup = new LogGroup(this, "ApiAccessLogGroup", new LogGroupProps
        {
            LogGroupName = $"/aws/apigateway/{AppConstants.StackPrefix}-HttpApi",
            Retention = ApiGatewayConstants.AccessLogRetentionDays,
            RemovalPolicy = RemovalPolicy.DESTROY,
        });

        var api = new HttpApi(this, nameof(this.HttpApi), new HttpApiProps
        {
            ApiName = $"{AppConstants.StackPrefix}-HttpApi",
            CreateDefaultStage = false,
        });

        _ = new HttpStage(this, "DefaultStage", new HttpStageProps
        {
            HttpApi = api,
            StageName = ApiGatewayConstants.StageName,
            AutoDeploy = true,
            AccessLogSettings = new HttpAccessLogSettings(new ApiGatewayLogDestination(accessLogGroup), AccessLogFormat.Custom(ApiGatewayConstants.AccessLogFormatJson).ToString()),
        });

        var authorizer = new HttpJwtAuthorizer("JwtAuthorizer", ApiGatewayConstants.JwtIssuer, new HttpJwtAuthorizerProps
        {
            JwtAudience = [ApiGatewayConstants.JwtAudience],
        });
        var sampleIntegration = new HttpLambdaIntegration("SampleIntegration", sampleFunction);

        api.AddRoutes(new AddRoutesOptions
        {
            Path = ApiGatewayConstants.SamplePath,
            Methods = [HttpApiMethod.GET, HttpApiMethod.POST],
            Integration = sampleIntegration,
            Authorizer = authorizer,
        });
        api.AddRoutes(new AddRoutesOptions
        {
            Path = ApiGatewayConstants.HealthPath,
            Methods = [HttpApiMethod.GET],
            Integration = sampleIntegration,
        });

        this.HttpApi = api;
        this.ApiEndpoint = api.ApiEndpoint;
        this.ApiDomainName = Fn.Select(2, Fn.Split("/", this.ApiEndpoint));

        _ = new CfnOutput(this, "ApiEndpoint", new CfnOutputProps
        {
            Description = "HTTP API endpoint",
            Value = this.ApiEndpoint,
        });
    }

    public string ApiDomainName { get; }

    public string ApiEndpoint { get; }

    public IHttpApi HttpApi { get; }

    private sealed class HttpAccessLogSettings : IAccessLogSettings
    {
        public HttpAccessLogSettings(ApiGatewayAccessLogDestination destination, string format)
        {
            this.Destination = destination;
            this.Format = format;
        }

        public ApiGatewayAccessLogDestination Destination { get; }

        public string Format { get; }
    }
}
