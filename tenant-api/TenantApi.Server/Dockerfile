# syntax=docker/dockerfile:1
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy repo-level build configuration so analyzers/props apply identically.
COPY Directory.Build.props Directory.Build.targets Analyzers.ruleset ./
COPY TenantApi.Server/ ./TenantApi.Server/

WORKDIR /src/TenantApi.Server
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TenantApi.Server.dll"]
