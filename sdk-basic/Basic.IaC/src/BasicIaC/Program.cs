namespace BasicIaC;

using Amazon.CDK;

internal static class Program
{
    public static void Main()
    {
        var app = new App();
        _ = new BasicIaCStack(app, "BasicIaCStack", new StackProps
        {
            // Env = new Amazon.CDK.Environment
            // {
            //     Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
            //     Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION"),
            // },
        });
        app.Synth();
    }
}
