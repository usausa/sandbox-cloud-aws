using BasicClient;

using Smart.CommandLine.Hosting;

var builder = CommandHost.CreateBuilder(args);
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("Aurora Serverless client tool");
    });

    commands.AddCommands();
});

var host = builder.Build();
return await host.RunAsync();
