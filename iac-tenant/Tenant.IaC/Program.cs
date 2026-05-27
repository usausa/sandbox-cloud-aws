using Amazon.CDK;
using Tenant.IaC.Constants;
using Tenant.IaC.Stacks;

var app = new App();

var env = new Amazon.CDK.Environment
{
    Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
    Region = AppConstants.Region,
};

Tags.Of(app).Add("Project", TaggingConstants.Project);
Tags.Of(app).Add("Environment", TaggingConstants.Environment);
Tags.Of(app).Add("ManagedBy", TaggingConstants.ManagedBy);

var network = new NetworkStack(app, $"{AppConstants.StackPrefix}-Network", new StackProps { Env = env });
var cognito = new CognitoStack(app, $"{AppConstants.StackPrefix}-Cognito", new StackProps { Env = env });
var database = new DatabaseStack(app, $"{AppConstants.StackPrefix}-Database", new StackProps { Env = env }, network);
var ecs = new EcsStack(app, $"{AppConstants.StackPrefix}-Ecs", new StackProps { Env = env }, network, database);
_ = new ApiGatewayStack(app, $"{AppConstants.StackPrefix}-ApiGateway", new StackProps { Env = env }, cognito, ecs);

app.Synth();
