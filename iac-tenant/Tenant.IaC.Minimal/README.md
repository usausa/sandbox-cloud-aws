# Tenant.IaC.Minimal — 簡易版 IaC プロジェクト

AWS CDK (C# .NET 10) による環境確認・PoC 用の最小構成。

## アーキテクチャ

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

## フル版との違い

| 機能 | フル版 | 簡易版 |
|------|--------|--------|
| 認証 | Cognito + JWT | なし |
| DB | Aurora Serverless v2 | なし |
| Lambda | Pre Token Generation V2 | なし |
| API | テナント管理 API | Ping のみ |
| VPC | Public + Private Isolated | Public のみ |
| VPC CIDR | `10.0.0.0/16` | `10.1.0.0/16` |

## 前提ツール

- AWS CLI (v2) + `aws configure`
- AWS CDK CLI: `npm install -g aws-cdk`
- .NET 10 SDK
- Docker Desktop
- PowerShell 7+

## 定数一覧 (簡易版)

| 定数 | デフォルト値 | TODO |
|------|------------|------|
| `MinimalConstants.Region` | `ap-northeast-1` | 要 |
| `MinimalConstants.StackPrefix` | `TenantMinimal` | 要 |
| `MinimalConstants.VpcCidr` | `10.1.0.0/16` | 任意 |
| `MinimalConstants.EcrRepositoryName` | `tenant-ping` | 任意 |
| `MinimalConstants.ClusterName` | `tenant-ping-cluster` | 任意 |
| `MinimalConstants.ApiName` | `tenant-ping-api` | 任意 |

## 環境変数

| 変数 | 用途 |
|------|------|
| `CDK_DEFAULT_ACCOUNT` | デプロイ先 AWS アカウント ID |

## デプロイ手順

```powershell
# 初回 Bootstrap
cd Tenant.IaC.Minimal
cdk bootstrap

# スタックデプロイ
cdk deploy

# ECR にイメージ push
../scripts/ecr-push-ping.ps1

# ECS 再起動
aws ecs update-service --cluster tenant-ping-cluster --service tenant-ping-service --force-new-deployment --region ap-northeast-1

# 動作確認
curl <ApiEndpoint>/ping
# => {"status":"ok","timestamp":"..."}
```

## 削除手順

```powershell
cd Tenant.IaC.Minimal
cdk destroy
# ※ ECR は RemovalPolicy=RETAIN のため手動削除が必要
aws ecr delete-repository --repository-name tenant-ping --force --region ap-northeast-1
```

## トラブルシュート

- **ECS Service 起動失敗**: ECR に初回イメージが存在しないと失敗する。`cdk deploy` 後に `scripts/ecr-push-ping.ps1` を実行してから Service を起動
- **Ping API 疎通不可**: ALB ヘルスチェックが `/ping` で 200 を返すことを確認。ECS タスクのログを CloudWatch で確認
