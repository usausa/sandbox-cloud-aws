# Serverless.IaC.Minimal

Minimal AWS CDK for .NET 10 sample infrastructure with Route 53, ACM, WAF, CloudFront, API Gateway, Lambda, DynamoDB, and CloudWatch.

## Differences from the full project

- No VPC or VPC endpoints.
- No SSM configuration stack.
- No audit bucket or CloudTrail stack.
- No SNS or email alarm subscriptions.
- WAF logging targets CloudWatch Logs.

## Notes

- Update the `Constants` values before production deployment.
- Replace `src\placeholder` with real .NET 10 Lambda artifacts before deployment.
