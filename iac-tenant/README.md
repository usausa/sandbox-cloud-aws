# Tenant IaC — マルチテナント Web API 基盤

AWS CDK (C# .NET 10) によるマルチテナント Web API 基盤の IaC プロジェクト群。
フル版 (Cognito + API Gateway + ECS + Aurora) と簡易版 (API Gateway + ECS Ping) の 2 プロジェクトを提供する。

---

## プロジェクト構成

| プロジェクト | 内容 |
|---|---|
| `Tenant.IaC` | フル版 IaC — Cognito + API Gateway + ECS + Aurora Serverless v2 |
| `Tenant.IaC.Minimal` | 簡易版 IaC — API Gateway + ECS (Ping API) |
| `Tenant.WebApi` | フル版 ECS タスクで動作する Web API (ASP.NET Core) |
| `Tenant.WebApi.Ping` | 簡易版 ECS タスクで動作する Ping API |

---

## アーキテクチャ

### フル版 (Tenant.IaC)

```
                ┌────────────────────────┐
                │  Cognito User Pool     │
                │  - custom:tenantId     │
                │  - Pre Token Gen V2    │
                └────────────┬───────────┘
                             │ JWT (TenantId 入り)
                             ▼
  ┌──────────┐     ┌─────────────────────┐     ┌──────────────┐
  │ Client   │────▶│ API Gateway HTTP API│────▶│ Public ALB   │
  └──────────┘     │ JWT Authorizer      │     └───────┬──────┘
                   └─────────────────────┘             │
                                                       ▼
                                               ┌──────────────┐
                                               │ ECS Fargate  │
                                               │ Public Subnet│
                                               │ Spot, 1 task │
                                               └───────┬──────┘
                                                       │
                                                       ▼
                                               ┌──────────────┐
                                               │ Aurora Srv v2│
                                               │ Private(Iso) │
                                               │ 0.5-1 ACU    │
                                               └──────────────┘
```

### 簡易版 (Tenant.IaC.Minimal)

```
  ┌──────────┐     ┌─────────────────────┐     ┌───────────────┐
  │ Client   │────▶│ API Gateway HTTP API│────▶│ Public ALB    │
  └──────────┘     │ (認証なし)          │     └───────┬───────┘
                   └─────────────────────┘             │
                                                       ▼
                                               ┌──────────────┐
                                               │ ECS Fargate  │
                                               │ Spot, 1 task │
                                               │ Ping API     │
                                               └──────────────┘
```

---

## 前提ツール

- [AWS CLI v2](https://docs.aws.amazon.com/cli/latest/userguide/install-cliv2.html) + `aws configure`
- [AWS CDK CLI](https://docs.aws.amazon.com/cdk/v2/guide/cli.html): `npm install -g aws-cdk`
- .NET 10 SDK
- Docker Desktop
- PowerShell 7+

---

## クイックスタート

### フル版デプロイ

```powershell
cd Tenant.IaC
cdk bootstrap
cdk deploy --all
../scripts/ecr-push.ps1
aws ecs update-service --cluster tenant-cluster --service tenant-webapi-service --force-new-deployment --region ap-northeast-1
```

### 簡易版デプロイ

```powershell
cd Tenant.IaC.Minimal
cdk bootstrap
cdk deploy
../scripts/ecr-push-ping.ps1
aws ecs update-service --cluster tenant-ping-cluster --service tenant-ping-service --force-new-deployment --region ap-northeast-1
curl <ApiEndpoint>/ping
```

---

## 詳細ドキュメント

- [Tenant.IaC README](Tenant.IaC/README.md) — フル版の詳細仕様・デプロイ手順
- [Tenant.IaC.Minimal README](Tenant.IaC.Minimal/README.md) — 簡易版の詳細仕様
- [__docs/実装指示書.md](__docs/実装指示書.md) — 実装仕様書
- [lambda/pre-token-generation/README.md](lambda/pre-token-generation/README.md) — Lambda 関数仕様

---

## コスト目安 (ap-northeast-1、24/7 稼働)

| 構成 | 月額目安 |
|------|---------|
| フル版 (Aurora + Fargate Spot + ALB + API GW + Cognito) | ~$65 |
| 簡易版 (Fargate Spot + ALB + API GW) | ~$20 |

> Aurora が最大コスト (~$43/月)。停止可能。
