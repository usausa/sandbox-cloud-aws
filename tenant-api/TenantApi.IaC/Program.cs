namespace TenantApi.IaC;

using Amazon.CDK;

internal static class Program
{
    public static void Main(string[] args)
    {
        var app = new App();

        _ = new MainStack(app, Constants.StackName, new StackProps
        {
            Env = new Amazon.CDK.Environment
            {
                Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                Region = Constants.Region,
            },
            Description = "Tenant API sample (API Gateway + ECS Fargate + Aurora Serverless v2)",
        });

        app.Synth();
    }
}
