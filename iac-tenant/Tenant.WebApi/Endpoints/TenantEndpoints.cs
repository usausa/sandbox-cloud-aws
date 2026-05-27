namespace Tenant.WebApi.Endpoints;

using System.Security.Claims;

using Npgsql;

internal static class TenantEndpoints
{
    internal static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tenants/me", async (ClaimsPrincipal user, IConfiguration config) =>
        {
            var tenantId = user.FindFirstValue("custom:tenantId");
            if (string.IsNullOrEmpty(tenantId))
            {
                return Results.Unauthorized();
            }

            var host = Environment.GetEnvironmentVariable("DB_HOST") ?? string.Empty;
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? string.Empty;
            var username = Environment.GetEnvironmentVariable("DB_USERNAME") ?? string.Empty;
            var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty;

            var connString = $"Host={host};Database={dbName};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";

            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync().ConfigureAwait(false);

            await using var cmd = new NpgsqlCommand(
                "SELECT id, name FROM tenants WHERE id = @tenantId LIMIT 1",
                conn);
            cmd.Parameters.AddWithValue("tenantId", tenantId);

            await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            if (await reader.ReadAsync().ConfigureAwait(false))
            {
                return Results.Ok(new
                {
                    id = reader.GetString(0),
                    name = reader.GetString(1),
                });
            }

            return Results.NotFound(new { message = $"Tenant '{tenantId}' not found." });
        }).RequireAuthorization();

        return app;
    }
}
