# sdk-basic — Aurora Serverless Sample

Aurora Serverless v2 (PostgreSQL) を AWS CDK でデプロイし、.NET クライアントから操作するサンプルです。

---

## プロジェクト構成

| プロジェクト | 内容 |
|---|---|
| `Basic.IaC` | AWS CDK (C#) — Aurora Serverless v2 インフラ定義 |
| `Basic.Client` | CLI ツール — Aurora への接続確認・RDS クラスター操作 |

---

## Basic.IaC — インフラのデプロイ

### 前提条件

- [AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/install-cliv2.html) がインストール済みで `aws configure` 設定済み
- [AWS CDK CLI](https://docs.aws.amazon.com/cdk/v2/guide/cli.html) がインストール済み (`npm install -g aws-cdk`)
- .NET 10 SDK

### 環境変数

| 変数 | 必須 | 説明 | デフォルト |
|---|---|---|---|
| `DB_PASSWORD` | **必須** | DB 管理者パスワード | — |
| `DB_USERNAME` | 任意 | DB 管理者ユーザー名 | `dbadmin` |
| `DB_NAME` | 任意 | デフォルトデータベース名 | `appdb` |

### デプロイ手順

```powershell
# 環境変数をセット
$env:DB_PASSWORD = "your-secure-password"
$env:DB_USERNAME = "dbadmin"   # 省略可
$env:DB_NAME     = "appdb"     # 省略可

cd Basic.IaC

# 初回のみ CDK bootstrap が必要
cdk bootstrap

# デプロイ
cdk deploy
```

デプロイ完了後、出力に `ClusterEndpoint` (Aurora エンドポイント) が表示されます。

### 削除

```powershell
cd Basic.IaC
cdk destroy
```

---

## Basic.Client — CLI ツール

Aurora Serverless への接続確認と RDS クラスター操作を行う CLI ツールです。

### 前提条件

- .NET 10 SDK
- AWS 認証情報が設定済み (`aws configure` または環境変数 `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY`)

### 環境変数

| 変数 | 必須 | 説明 | デフォルト |
|---|---|---|---|
| `DB_HOST` | 任意 | Aurora クラスターエンドポイント | `localhost` |
| `DB_USERNAME` | 任意 | DB ユーザー名 | `dbadmin` |
| `DB_PASSWORD` | 任意 | DB パスワード | (空文字) |
| `DB_NAME` | 任意 | データベース名 | `appdb` |
| `AWS_DEFAULT_REGION` | 任意 | AWS リージョン | `ap-northeast-1` |

### ビルドと実行

```powershell
cd Basic.Client
dotnet build
dotnet run -- <command> [options]
```

### コマンド一覧

#### `connect` — Npgsql で Aurora に接続

Npgsql を直接使用して Aurora Serverless に接続し、サーバーバージョンを確認します。

```powershell
dotnet run -- connect [options]
# オプション
#   --host      Aurora エンドポイント (既定: DB_HOST 環境変数)
#   --port      ポート番号             (既定: 5432)
#   --user      ユーザー名             (既定: DB_USERNAME 環境変数)
#   --password  パスワード             (既定: DB_PASSWORD 環境変数)
#   --database  データベース名         (既定: DB_NAME 環境変数)

# 実行例
$env:DB_HOST     = "your-cluster.cluster-xxxx.ap-northeast-1.rds.amazonaws.com"
$env:DB_PASSWORD = "your-secure-password"
dotnet run -- connect
```

#### `connect2` — AWS Advanced .NET Data Provider Wrapper で接続

[AWS Advanced .NET Data Provider Wrapper](https://github.com/aws/aws-advanced-dotnet-wrapper) を使用して接続します。フェイルオーバー対応などの拡張機能を利用できます。

```powershell
dotnet run -- connect2 [options]
# オプションは connect と同じ
```

#### `list` — クラスター一覧とステータス表示

AWS SDK を使用して Aurora DB クラスターとインスタンスの一覧・ステータスを表示します。

```powershell
dotnet run -- list [--region <region>]

# 実行例
dotnet run -- list --region ap-northeast-1
```

出力例:
```
Cluster: basiciacstack-auroracluster...  Status: available  Engine: aurora-postgresql 16.6
  Instance: basiciacstack-writer...  Role: Writer  Status: available
```

#### `down` — クラスター停止

Aurora DB クラスターを停止します。

```powershell
dotnet run -- down --cluster-id <cluster-identifier> [--region <region>]

# 実行例
dotnet run -- down --cluster-id basiciacstack-auroracluster-xxxx
```

#### `up` — クラスター起動

停止中の Aurora DB クラスターを起動します。

```powershell
dotnet run -- up --cluster-id <cluster-identifier> [--region <region>]

# 実行例
dotnet run -- up --cluster-id basiciacstack-auroracluster-xxxx
```

---

## Aurora Serverless v2 構成詳細

| 項目 | 設定値 |
|---|---|
| エンジン | Aurora PostgreSQL 16.6 |
| インスタンス | Serverless v2 Writer のみ |
| ACU | 最小 0.5 〜 最大 1 |
| ネットワーク | パブリックサブネット (NAT なし) |
| ポート | 5432 (全 IP 許可) |
| ストレージ暗号化 | 無効 |
| Performance Insights | 無効 |
| CloudWatch ログ保持 | 1週間 |
| 削除保護 | 無効 |
