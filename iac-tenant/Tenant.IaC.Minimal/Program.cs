using Amazon.CDK;
using Tenant.IaC.Minimal.Constants;
using Tenant.IaC.Minimal.Stacks;

var app = new App();

var env = new Amazon.CDK.Environment
{
    Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
    Region = MinimalConstants.Region,
};

Tags.Of(app).Add("Project", MinimalConstants.TagProject);
Tags.Of(app).Add("Environment", MinimalConstants.TagEnvironment);
Tags.Of(app).Add("ManagedBy", MinimalConstants.TagManagedBy);

_ = new MinimalStack(app, $"{MinimalConstants.StackPrefix}-Stack", new StackProps { Env = env });

app.Synth();
