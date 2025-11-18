using Amazon.CDK;
using Amazon.CDK.AWS.EC2;

using Constructs;

using Serverless.IaC.Configuration;

namespace Serverless.IaC.Stacks;

/// <summary>
/// VPC とネットワーク関連リソース。
/// 外部接続で固定 IP が必要な Lambda 用に、Private Subnet + NAT Gateway + VPC Endpoint を構成する。
/// </summary>
internal sealed class NetworkStack : Stack
{
    public NetworkStack(Construct scope, string id, IStackProps props)
        : base(scope, id, props)
    {
        Vpc = new Vpc(this, "Vpc", new VpcProps
        {
            VpcName = $"{ProjectConstants.ResourceSuffix}-vpc",
            IpAddresses = IpAddresses.Cidr(ProjectConstants.VpcCidr),
            MaxAzs = ProjectConstants.VpcMaxAzs,
            NatGateways = ProjectConstants.NatGatewayCount,
            SubnetConfiguration =
            [
                new SubnetConfiguration
                {
                    Name = "public",
                    SubnetType = SubnetType.PUBLIC,
                    CidrMask = 24,
                },
                new SubnetConfiguration
                {
                    Name = "private",
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS,
                    CidrMask = 22,
                },
            ],
        });

        // Lambda 用 SG: アウトバウンドのみ
        LambdaSecurityGroup = new SecurityGroup(this, "LambdaSg", new SecurityGroupProps
        {
            Vpc = Vpc,
            SecurityGroupName = $"{ProjectConstants.ResourceSuffix}-lambda-sg",
            Description = "Security group for VPC-attached Lambda functions",
            AllowAllOutbound = true,
        });

        // DynamoDB Gateway Endpoint(NAT 回避でコスト削減)
        Vpc.AddGatewayEndpoint("DynamoDbEndpoint", new GatewayVpcEndpointOptions
        {
            Service = GatewayVpcEndpointAwsService.DYNAMODB,
        });

        // CloudWatch Logs Interface Endpoint
        Vpc.AddInterfaceEndpoint("LogsEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.CLOUDWATCH_LOGS,
            PrivateDnsEnabled = true,
            SecurityGroups = [LambdaSecurityGroup],
        });
    }

    public IVpc Vpc { get; }

    public ISecurityGroup LambdaSecurityGroup { get; }
}
