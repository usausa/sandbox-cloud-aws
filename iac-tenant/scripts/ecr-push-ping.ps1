# Tenant.WebApi.Ping を ECR にビルド & プッシュする
param(
    [string]$Region = "ap-northeast-1",
    [string]$RepositoryName = "tenant-ping",
    [string]$Tag = "latest"
)

$ErrorActionPreference = "Stop"

$accountId = (aws sts get-caller-identity --query Account --output text)
$registry = "$accountId.dkr.ecr.$Region.amazonaws.com"
$imageUri = "$registry/${RepositoryName}:${Tag}"

Write-Host "Logging in to ECR..." -ForegroundColor Cyan
aws ecr get-login-password --region $Region | docker login --username AWS --password-stdin $registry

Push-Location (Join-Path $PSScriptRoot "..\Tenant.WebApi.Ping")
try {
    Write-Host "Building image..." -ForegroundColor Cyan
    docker build --platform linux/amd64 -t $imageUri .

    Write-Host "Pushing image..." -ForegroundColor Cyan
    docker push $imageUri

    Write-Host "Image pushed: $imageUri" -ForegroundColor Green
}
finally {
    Pop-Location
}
