# Tenant API サンプル

API Gateway + ECS Fargate + Aurora Serverless v2 を使ったマルチテナント API のサンプルです。

---

## アーキテクチャ概要

Client が Cognito で取得したアクセストークンを API Gateway に渡すと、JWT Authorizer で検証後、
X-Tenant-Id ヘッダーに変換して VPC Link 経由で ECS (Fargate Spot) に転送されます。
ECS 上の .NET 10 Minimal API が Aurora Serverless v2 (PostgreSQL) からテナント名を取得して返します。

---

## プロジェクト構成

```
tenant-api/
├── TenantApi.Server/             # .NET 10 ECS アプリケーション
│   ├── Program.cs
│   ├── Dockerfile
│   ├── appsettings.json
│   └── appsettings.Development.json   <- .gitignore 対象 (接続文字列を記載)
├── TenantApi.IaC/                # AWS CDK (C#) IaC プロジェクト
│   ├── Constants.cs              # 設定値を集約
│   ├── MainStack.cs
│   └── Constructs/
│       ├── NetworkConstruct.cs   # VPC / SG / VPC Endpoint
│       ├── DatabaseConstruct.cs  # Aurora Serverless v2
│       ├── AuthConstruct.cs      # Cognito / Lambda Trigger
│       ├── ContainerConstruct.cs # ECS / ECR / Cloud Map
│       ├── ApiConstruct.cs       # API Gateway HTTP API
│       └── SchedulerConstruct.cs # EventBridge Scheduler
├── lambda/
│   ├── pre-token-generation/index.mjs  # Cognito トークントリガー
│   ├── start-environment/index.mjs     # ECS / Aurora 起動
│   └── stop-environment/index.mjs      # ECS / Aurora 停止
├── cdk.json
└── README.md
```

---

## 前提条件

| ツール        | バージョン                                   |
|-------------|---------------------------------------------|
| AWS CLI     | v2 以上                                      |
| AWS CDK CLI | v2 (npm install -g aws-cdk)                 |
| .NET SDK    | 10.0 以上                                    |
| Docker      | 起動済み (CDK がイメージを自動 Build & Push)    |

AWS CLI の認証情報が設定されていること (aws configure または環境変数)。

---

## デプロイ手順

### 1. CDK Bootstrap (初回のみ)

CDK 用の S3 バケット等を AWS アカウントに作成します。

```bash
cdk bootstrap aws://<ACCOUNT_ID>/ap-northeast-1
```

### 2. 初回デプロイ

Aurora エンドポイントはデプロイ後に確定するため、まず仮設定でデプロイします。

```bash
cdk deploy
```

デプロイ完了後の出力値を確認します。

| 出力キー           | 内容                          |
|------------------|-------------------------------|
| HttpApiUrl       | API Gateway エンドポイント URL  |
| UserPoolId       | Cognito User Pool ID          |
| UserPoolClientId | Cognito App Client ID         |
| AuroraEndpoint   | Aurora クラスターエンドポイント  |
| AuroraSecretArn  | Secrets Manager 認証情報 ARN  |

### 3. 接続文字列を設定して再デプロイ

TenantApi.Server/appsettings.Development.json の接続文字列を更新します。
パスワードは Secrets Manager から取得します。

```bash
aws secretsmanager get-secret-value --secret-id <AuroraSecretArn> --query SecretString --output text
```

```json
{
  "ConnectionStrings": {
    "Aurora": "Host=<AuroraEndpoint>;Port=5432;Database=mydb;Username=postgres;Password=<PASSWORD>;SSL Mode=Require;Trust Server Certificate=true;Maximum Pool Size=20;Connection Idle Lifetime=300;Timeout=30"
  }
}
```

注意: このファイルは .gitignore に追加してリポジトリへコミットしないようにしてください。

```bash
cdk deploy
```

---

## CDK 以外の必要作業

### Aurora 初期データの投入

Aurora は VPC プライベートサブネット内にあるため、EC2 踏み台 (SSM Session Manager) 経由で接続します。

1. Aurora と同じ VPC サブネットに EC2 (Amazon Linux) を一時起動
2. SSM Session Manager でログイン
3. psql で Aurora に接続してテーブルとデータを作成

```sql
CREATE TABLE tenants (
    tenant_id VARCHAR(64) PRIMARY KEY,
    name      VARCHAR(256) NOT NULL
);

INSERT INTO tenants (tenant_id, name) VALUES
  ('tenant-001', 'サンプル株式会社'),
  ('tenant-002', 'テスト工業');
```

### Cognito テストユーザーの作成

```bash
USER_POOL_ID=<UserPoolId>
USERNAME=testuser@example.com
TENANT_ID=tenant-001

# ユーザー作成
aws cognito-idp admin-create-user \
  --user-pool-id  \
  --username  \
  --temporary-password "Temp1234!" \
  --message-action SUPPRESS

# パスワードを確定状態にする
aws cognito-idp admin-set-user-password \
  --user-pool-id  \
  --username  \
  --password "Permanent1234!" \
  --permanent

# テナントID カスタム属性を設定
aws cognito-idp admin-update-user-attributes \
  --user-pool-id  \
  --username  \
  --user-attributes Name=custom:tenant_id,Value=
```

---

## 動作確認

### TestClient を使用する場合（推奨）

`TenantApi.TestClient` は Cognito 認証 → `/health` → `/tenant` を自動で実行する .NET コンソールアプリです。

#### 1. appsettings.json を編集

`TenantApi.TestClient/appsettings.json` の `REPLACE_ME` を以下の値で置き換えます。

| キー | 確認方法 |
|---|---|
| `Cognito:Region` | デプロイ先リージョン (`ap-northeast-1` など) |
| `Cognito:UserPoolId` | `cdk deploy` 出力 `TenantApiStack.UserPoolId` または CloudFormation → スタック → 出力タブ |
| `Cognito:ClientId` | `cdk deploy` 出力 `TenantApiStack.UserPoolClientId` または同上 |
| `Api:BaseUrl` | `cdk deploy` 出力 `TenantApiStack.HttpApiUrl` または同上 |
| `TestUser:Username` | 後述の手順で作成した Cognito ユーザー名 |
| `TestUser:Password` | 同ユーザーの確定パスワード |

```json
{
  "Cognito": {
    "Region": "ap-northeast-1",
    "UserPoolId": "ap-northeast-1_XXXXXXXXX",
    "ClientId": "XXXXXXXXXXXXXXXXXXXXXXXXXX"
  },
  "Api": {
    "BaseUrl": "https://XXXXXXXXXX.execute-api.ap-northeast-1.amazonaws.com"
  },
  "TestUser": {
    "Username": "testuser@example.com",
    "Password": "Permanent1234!"
  }
}
```

#### 2. 実行

```bash
cd TenantApi.TestClient
dotnet run
```

実行例:

```
=== TenantApi Test Client ===
User Pool : ap-northeast-1_XXXXXXXXX
API       : https://XXXXXXXXXX.execute-api.ap-northeast-1.amazonaws.com

[1] Authenticating...
    Access token acquired.

[2] GET /health
    Status : 200 OK
    Body   : Healthy

[3] GET /tenant
    Status : 200 OK
    Body   : {"tenantId":"tenant-001","name":"サンプル株式会社"}
```

---

### curl を使用する場合

アクセストークンは Postman の OAuth 2.0 認証 (Authorization Code) または AWS CLI の cognito-idp コマンドで取得します。

```bash
curl -H "Authorization: Bearer <ACCESS_TOKEN>" "<HttpApiUrl>/tenant"
```

レスポンス例 (200 OK):

```json
{ "tenantId": "tenant-001", "name": "サンプル株式会社" }
```

ヘルスチェック (認証不要):

```bash
curl "<HttpApiUrl>/health"
```

| ステータス | 意味                                    |
|----------|-----------------------------------------|
| 200      | テナント情報を返却                          |
| 400      | X-Tenant-Id ヘッダーなし                  |
| 401      | アクセストークンが無効または期限切れ          |
| 404      | 該当テナントが DB に存在しない               |

---

## 夜間停止スケジュール

EventBridge Scheduler により平日のみ自動起動・停止します。

| スケジュール  | JST        | 内容                           |
|------------|------------|-------------------------------|
| Aurora 起動 | 平日 09:00 | StartDBCluster                |
| ECS 起動   | 平日 09:05 | UpdateService desiredCount=1  |
| ECS 停止   | 平日 21:00 | UpdateService desiredCount=0  |
| Aurora 停止 | 平日 21:01 | StopDBCluster                 |

Aurora の StopDBCluster は AWS の仕様で最大 7 日後に自動起動します。週末をまたぐ場合は月曜朝のスケジュールで制御されます。

手動で起動・停止する場合は start-environment / stop-environment Lambda を直接 Invoke してください。

```bash
aws lambda invoke --function-name <StartLambdaName> --payload '{"action":"start-db"}' /dev/null
aws lambda invoke --function-name <StartLambdaName> --payload '{"action":"start-ecs"}' /dev/null
```

---

## リソース削除

```bash
cdk destroy
```

RemovalPolicy.DESTROY が設定されているため Aurora / Cognito User Pool / CloudWatch Logs グループも削除されます。

---

## カスタマイズ

TenantApi.IaC/Constants.cs に設定値を集約しています。変更が必要な主な定数:

| 定数                                   | 内容                          |
|---------------------------------------|-------------------------------|
| Region                                | デプロイリージョン               |
| AvailabilityZone                      | AZ                            |
| VpcCidr                               | VPC CIDR                      |
| DbMinAcu / DbMaxAcu                   | Aurora キャパシティ範囲          |
| ContainerCpu / ContainerMemoryMiB     | ECS タスクサイズ                |
| ScheduleAuroraStart など              | 起動停止スケジュール (UTC cron)  |
