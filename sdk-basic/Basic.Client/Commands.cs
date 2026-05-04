namespace BasicClient;

using Amazon.RDS;
using Amazon.RDS.Model;

using AwsWrapperDataProvider;
using AwsWrapperDataProvider.Dialect.Npgsql;

using Npgsql;

using Smart.CommandLine.Hosting;

//--------------------------------------------------------------------------------
// Command builder
//--------------------------------------------------------------------------------
public static class CommandBuilderExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<ConnectCommand>();
        commands.AddCommand<Connect2Command>();
        commands.AddCommand<ListCommand>();
        commands.AddCommand<DownCommand>();
        commands.AddCommand<UpCommand>();
    }
}

//--------------------------------------------------------------------------------
// connect - Npgsql direct connection
//--------------------------------------------------------------------------------
[Command("connect", "Connect to Aurora Serverless via Npgsql and run a test query")]
public sealed class ConnectCommand : ICommandHandler
{
    [Option("--host", "Aurora cluster endpoint host")]
    public string Host { get; set; } = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";

    [Option("--port", "PostgreSQL port")]
    public int Port { get; set; } = 5432;

    [Option("--user", "Database user")]
    public string User { get; set; } = Environment.GetEnvironmentVariable("DB_USERNAME") ?? "dbadmin";

    [Option("--password", "Database password")]
    public string Password { get; set; } = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty;

    [Option("--database", "Database name")]
    public string Database { get; set; } = Environment.GetEnvironmentVariable("DB_NAME") ?? "appdb";

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Username = User,
            Password = Password,
            Database = Database,
            SslMode = SslMode.Prefer,
        }.ConnectionString;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(context.CancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT version()";
        var result = await command.ExecuteScalarAsync(context.CancellationToken);

        Console.WriteLine($"Connected via Npgsql.");
        Console.WriteLine($"Server version: {result}");
    }
}

//--------------------------------------------------------------------------------
// connect2 - AWS Advanced .NET Data Provider Wrapper connection
//--------------------------------------------------------------------------------
[Command("connect2", "Connect to Aurora Serverless via AWS Advanced .NET Data Provider Wrapper")]
public sealed class Connect2Command : ICommandHandler
{
    [Option("--host", "Aurora cluster endpoint host")]
    public string Host { get; set; } = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";

    [Option("--port", "PostgreSQL port")]
    public int Port { get; set; } = 5432;

    [Option("--user", "Database user")]
    public string User { get; set; } = Environment.GetEnvironmentVariable("DB_USERNAME") ?? "dbadmin";

    [Option("--password", "Database password")]
    public string Password { get; set; } = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty;

    [Option("--database", "Database name")]
    public string Database { get; set; } = Environment.GetEnvironmentVariable("DB_NAME") ?? "appdb";

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        NpgsqlDialectLoader.Load();

        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Username = User,
            Password = Password,
            Database = Database,
            SslMode = SslMode.Prefer,
        }.ConnectionString;

        await using var connection = new AwsWrapperConnection(connectionString);
        await connection.OpenAsync(context.CancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT version()";
        var result = await command.ExecuteScalarAsync(context.CancellationToken);

        Console.WriteLine($"Connected via AWS Advanced .NET Data Provider Wrapper.");
        Console.WriteLine($"Server version: {result}");
    }
}

//--------------------------------------------------------------------------------
// list - List Aurora clusters and instances with status
//--------------------------------------------------------------------------------
[Command("list", "List Aurora DB clusters and their instance statuses")]
public sealed class ListCommand : ICommandHandler
{
    [Option("--region", "AWS region")]
    public string Region { get; set; } = Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION") ?? "ap-northeast-1";

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        using var client = new AmazonRDSClient(Amazon.RegionEndpoint.GetBySystemName(Region));

        var clustersResponse = await client.DescribeDBClustersAsync(new DescribeDBClustersRequest(), context.CancellationToken);

        if (clustersResponse.DBClusters.Count == 0)
        {
            Console.WriteLine("No DB clusters found.");
            return;
        }

        foreach (var cluster in clustersResponse.DBClusters)
        {
            Console.WriteLine($"Cluster: {cluster.DBClusterIdentifier}  Status: {cluster.Status}  Engine: {cluster.Engine} {cluster.EngineVersion}");

            foreach (var member in cluster.DBClusterMembers)
            {
                var instancesResponse = await client.DescribeDBInstancesAsync(
                    new DescribeDBInstancesRequest { DBInstanceIdentifier = member.DBInstanceIdentifier },
                    context.CancellationToken);

                var instance = instancesResponse.DBInstances.FirstOrDefault();
                var role = member.IsClusterWriter == true ? "Writer" : "Reader";
                var status = instance?.DBInstanceStatus ?? "unknown";
                Console.WriteLine($"  Instance: {member.DBInstanceIdentifier}  Role: {role}  Status: {status}");
            }
        }
    }
}

//--------------------------------------------------------------------------------
// down - Stop Aurora cluster instances
//--------------------------------------------------------------------------------
[Command("down", "Stop an Aurora DB cluster")]
public sealed class DownCommand : ICommandHandler
{
    [Option("--cluster-id", "DB cluster identifier to stop")]
    public string ClusterId { get; set; } = string.Empty;

    [Option("--region", "AWS region")]
    public string Region { get; set; } = Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION") ?? "ap-northeast-1";

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        using var client = new AmazonRDSClient(Amazon.RegionEndpoint.GetBySystemName(Region));

        Console.WriteLine($"Stopping cluster: {ClusterId}");
        await client.StopDBClusterAsync(new StopDBClusterRequest { DBClusterIdentifier = ClusterId }, context.CancellationToken);
        Console.WriteLine("Stop request sent. Use 'list' to check status.");
    }
}

//--------------------------------------------------------------------------------
// up - Start Aurora cluster instances
//--------------------------------------------------------------------------------
[Command("up", "Start an Aurora DB cluster")]
public sealed class UpCommand : ICommandHandler
{
    [Option("--cluster-id", "DB cluster identifier to start")]
    public string ClusterId { get; set; } = string.Empty;

    [Option("--region", "AWS region")]
    public string Region { get; set; } = Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION") ?? "ap-northeast-1";

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        using var client = new AmazonRDSClient(Amazon.RegionEndpoint.GetBySystemName(Region));

        Console.WriteLine($"Starting cluster: {ClusterId}");
        await client.StartDBClusterAsync(new StartDBClusterRequest { DBClusterIdentifier = ClusterId }, context.CancellationToken);
        Console.WriteLine("Start request sent. Use 'list' to check status.");
    }
}
