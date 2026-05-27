# Pre Token Generation Lambda

Cognito User Pool の Pre Token Generation V2 トリガーとして使用する Lambda 関数。

## 目的

サインイン時に発行される **Access Token** および ID Token に `custom:tenantId` クレームを追加する。
Cognito 標準では Access Token にカスタム属性は含まれないため、V2 トリガーで上書きする。

## 仕様

- ランタイム: Node.js 22.x (ES Module)
- ハンドラー: `index.handler`
- トリガーバージョン: **V2_0** (Access Token カスタマイズに必須)

## ロジック

1. `event.request.userAttributes["custom:tenantId"]` を取得
2. 値が存在する場合、Access Token / ID Token の両方に `custom:tenantId` クレームを追加
3. `event` をそのまま返す

## デプロイ

CDK の `CognitoStack` から `Code.FromAsset("../lambda/pre-token-generation")` でバンドル・デプロイされる。
追加の npm パッケージは不要。
