namespace Tenant.WebApi.Endpoints;

internal static class HealthEndpoints
{
    internal static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok()).AllowAnonymous();
        return app;
    }
}
