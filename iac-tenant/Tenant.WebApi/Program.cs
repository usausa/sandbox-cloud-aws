using Microsoft.AspNetCore.Authentication.JwtBearer;
using Tenant.WebApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "ap-northeast-1";
var userPoolId = Environment.GetEnvironmentVariable("COGNITO_USER_POOL_ID") ?? string.Empty;
var clientId = Environment.GetEnvironmentVariable("COGNITO_CLIENT_ID") ?? string.Empty;
var authority = $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.Audience = clientId;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = true,
            ValidAudience = clientId,
            ValidateLifetime = true,
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapTenantEndpoints();

app.Run();
