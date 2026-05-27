# Tenant.IaC — フル版 IaC プロジェクト

AWS CDK (C# .NET 10) による本番想定のマルチテナント Web API 基盤。

## アーキテクチャ

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

> VPC Link は使用せず、API Gateway HTTP API → internet-facing ALB に直接統合する (コスト削減)。

## ディレクトリ構成

```
Tenant.IaC/
├── Tenant.IaC.csproj
├── Program.cs
├── cdk.json
├── Constants/
│   ├── AppConstants.cs          # リージョン、スタックプレフィックス
│   ├── TaggingConstants.cs      # 共通タグ
│   ├── NetworkConstants.cs      # VPC/Subnet/CIDR
│   ├── CognitoConstants.cs      # User Pool 設定
│   ├── EcsConstants.cs          # Fargate, ECR
│   ├── DatabaseConstants.cs     # Aurora 設定
│   └── ApiGatewayConstants.cs   # HTTP API 設定
└── Stacks/
    ├── NetworkStack.cs
    ├── CognitoStack.cs
    ├── DatabaseStack.cs
    ├── EcsStack.cs
    └── ApiGatewayStack.cs
```

## 前提ツール

- AWS CLI (v2) + `aws configure`
- AWS CDK CLI: `npm install -g aws-cdk`
- .NET 10 SDK
- Docker Desktop
- PowerShell 7+

## 必要 IAM 権限

デプロイ実行ユーザーは以下のサービスへのフル権限が必要:
`cloudformation`, `ec2`, `cognito-idp`, `lambda`, `rds`, `ecs`, `ecr`, `elasticloadbalancing`, `apigateway`, `secretsmanager`, `logs`, `iam`

## サービス別構成

### Network
- VPC CIDR: `10.0.0.0/16`、AZ: 2 (`ap-northeast-1a`, `ap-northeast-1c`)
- Public Subnet: `10.0.0.0/24`, `10.0.1.0/24` — ALB・ECS タスク配置
- Private (Isolated) Subnet: `10.0.10.0/24`, `10.0.11.0/24` — Aurora 配置
- **NAT Gateway なし** (コスト削減。ECS は Public Subnet + Public IP で ECR に到達)
- AlbSg: 0.0.0.0/0 → TCP:80 / EcsSg: AlbSg → TCP:8080 / AuroraSg: EcsSg → TCP:5432

### Cognito
- User Pool: Email サインイン、MFA Optional、カスタム属性 `tenantId`
- Pre Token Generation V2 Lambda — Access Token / ID Token に `custom:tenantId` を追加
- User Pool Client: `USER_SRP_AUTH` + `USER_PASSWORD_AUTH`、シークレットなし

### Database
- Aurora Serverless v2 PostgreSQL 16.6
- ACU: 0.5 (Min) — 1.0 (Max)
- 認証情報: Secrets Manager で自動生成 (`tenant-aurora-credentials`)
- 開発設定: RemovalPolicy=DESTROY、削除保護無効

### ECS
- ECR: `tenant-webapi`、ライフサイクルポリシー最新5件、RemovalPolicy=RETAIN
- Cluster: `tenant-cluster`、Fargate Spot (weight=1)
- Task: CPU 256 / Mem 512 MB、Public Subnet、AssignPublicIp=true
- ALB: internet-facing、ヘルスチェック `/health`

### API Gateway
- HTTP API + JWT Authorizer (Cognito User Pool)
- Route: `ANY /{proxy+}` → HTTP_PROXY (ALB DNS 名)
- **VPC Link 不使用** (Public ALB に直接統合、月数ドルのコスト削減)

## 定数一覧

| 定数 | デフォルト値 | TODO |
|------|------------|------|
| `AppConstants.StackPrefix` | `Tenant` | 要 |
| `AppConstants.Region` | `ap-northeast-1` | 要 |
| `TaggingConstants.Environment` | `dev` | 要 |
| `NetworkConstants.VpcCidr` | `10.0.0.0/16` | 任意 |
| `CognitoConstants.UserPoolName` | `tenant-user-pool` | 任意 |
| `DatabaseConstants.ClusterIdentifier` | `tenant-aurora` | 任意 |
| `DatabaseConstants.MinAcu` | `0.5` | 任意 |
| `DatabaseConstants.MaxAcu` | `1.0` | 要 (負荷次第) |
| `DatabaseConstants.BackupRetentionDays` | `1` | 要 (本番は7日以上) |
| `EcsConstants.EcrRepositoryName` | `tenant-webapi` | 任意 |
| `EcsConstants.ImageTag` | `latest` | 要 (本番はイミュータブルタグ) |
| `ApiGatewayConstants.ApiName` | `tenant-api` | 任意 |

## 環境変数

| 変数 | 用途 |
|------|------|
| `CDK_DEFAULT_ACCOUNT` | デプロイ先 AWS アカウント ID |

## デプロイ手順

```powershell
# 初回 Bootstrap
cd Tenant.IaC
cdk bootstrap

# 全スタックデプロイ
cdk deploy --all

# ECR にイメージ push
../scripts/ecr-push.ps1

# ECS 再起動
aws ecs update-service --cluster tenant-cluster --service tenant-webapi-service --force-new-deployment --region ap-northeast-1

# Cognito ユーザー作成 (custom:tenantId 付与)
aws cognito-idp admin-create-user --user-pool-id <UserPoolId> --username user@example.com --user-attributes Name=email,Value=user@example.com Name=custom:tenantId,Value=tenant-001 --region ap-northeast-1

# トークン取得
aws cognito-idp initiate-auth --auth-flow USER_PASSWORD_AUTH --auth-parameters USERNAME=user@example.com,PASSWORD=<Password> --client-id <ClientId> --region ap-northeast-1

# 動作確認
curl <ApiEndpoint>/api/tenants/me -H "Authorization: Bearer <AccessToken>"
```

## 削除手順

```powershell
cd Tenant.IaC
cdk destroy --all
# ※ ECR は RemovalPolicy=RETAIN のため手動削除が必要
aws ecr delete-repository --repository-name tenant-webapi --force --region ap-northeast-1
```

## トラブルシュート

- **Pre Token Generation V2**: CDK escape hatch (`CfnUserPool.LambdaConfig`) で設定。CDK ネイティブ API の `lambdaTriggers` は V1 のみ対応
- **ECS Service 起動失敗**: ECR に初回イメージが存在しないと失敗する。`cdk deploy Tenant-Ecs` 後に `scripts/ecr-push.ps1` を実行してから Service を起動
- **Aurora への接続失敗**: EcsSg → AuroraSg の 5432 ポート開放と Secrets Manager の値を確認
- **JWT 401**: Access Token の `iss` が User Pool の Issuer URL と一致しているか確認。Pre Token Gen V2 トリガーが正しく設定されているか確認
