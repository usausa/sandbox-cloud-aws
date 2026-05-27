var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/ping", () => new { status = "ok", timestamp = DateTime.UtcNow });
app.MapGet("/health", () => Results.Ok());

app.Run();
