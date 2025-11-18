using Dapper;

using Npgsql;

using TenantApi.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Aurora");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:Aurora is not configured.");
}

builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Health check (used by ECS).
app.MapGet("/health", static () => Results.Ok(new { status = "ok" }));

// Returns the tenant name for the tenant id supplied by API Gateway via the X-Tenant-Id header.
app.MapGet("/tenant", static async (HttpContext context, NpgsqlDataSource dataSource, CancellationToken cancellationToken) =>
{
    if (!context.Request.Headers.TryGetValue("X-Tenant-Id", out var values) ||
        string.IsNullOrWhiteSpace(values.ToString()))
    {
        return Results.BadRequest(new { error = "X-Tenant-Id header is required." });
    }

    var tenantId = values.ToString();

    await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

    var command = new CommandDefinition(
        "SELECT tenant_id AS TenantId, name AS Name FROM tenants WHERE tenant_id = @TenantId",
        new { TenantId = tenantId },
        cancellationToken: cancellationToken);

    var tenant = await connection.QuerySingleOrDefaultAsync<Tenant>(command).ConfigureAwait(false);

    return tenant is null
        ? Results.NotFound(new { tenantId })
        : Results.Ok(tenant);
})
.WithName("GetTenant");

app.Run();

namespace TenantApi.Server
{
    internal sealed record Tenant(string TenantId, string Name);
}
