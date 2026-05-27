# Serverless.IaC

AWS CDK for .NET 10 sample infrastructure with Route 53, ACM, CloudFront, WAF, API Gateway, Lambda, DynamoDB, SSM, CloudWatch, and CloudTrail.

## Stacks

- `DnsStack`: Creates or imports the hosted zone and exposes `AddAliasRecord()`.
- `CertificateStack`: Creates the CloudFront certificate in `us-east-1`.
- `WafStack`: Creates the CloudFront Web ACL in `us-east-1`.
- `NetworkStack`: Provisions VPC, subnets, NAT gateways, and VPC endpoints.
- `DataStack`: Creates the sample DynamoDB table.
- `ConfigStack`: Creates the SSM parameter root.
- `StorageStack`: Creates SPA, audit, and SAM artifact buckets.
- `ComputeStack`: Creates placeholder .NET 10 Lambda functions backed by a placeholder asset directory.
- `ApiStack`: Creates the HTTP API routes and JWT authorizer.
- `EdgeStack`: Creates the CloudFront distribution for SPA and API traffic.
- `ObservabilityStack`: Creates dashboards and alarms.
- `AuditStack`: Creates the CloudTrail trail.

## Notes

- Update the `Constants` values before production deployment.
- Replace `src\placeholder` with real .NET 10 Lambda artifacts before deployment.
- The CloudFront certificate and WAF Web ACL are created in `us-east-1` because CloudFront requires it.
