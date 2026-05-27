// <copyright file="NetworkStack.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Stacks;

using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;
using Serverless.IaC.Constants;

internal sealed class NetworkStack : Stack
{
    public NetworkStack(Construct scope, string id, IStackProps? props = null)
        : base(scope, id, props)
    {
        this.Vpc = new Vpc(this, nameof(this.Vpc), new VpcProps
        {
            VpcName = $"{AppConstants.StackPrefix}-Vpc",
            IpAddresses = IpAddresses.Cidr(NetworkConstants.VpcCidr),
            MaxAzs = NetworkConstants.MaxAzs,
            NatGateways = NetworkConstants.NatGateways,
            SubnetConfiguration =
            [
                new SubnetConfiguration { Name = "Public", SubnetType = SubnetType.PUBLIC, CidrMask = NetworkConstants.PublicSubnetCidrMask },
                new SubnetConfiguration { Name = "Private", SubnetType = SubnetType.PRIVATE_WITH_EGRESS, CidrMask = NetworkConstants.PrivateSubnetCidrMask },
            ],
        });

        this.LambdaSecurityGroup = new SecurityGroup(this, nameof(this.LambdaSecurityGroup), new SecurityGroupProps
        {
            Vpc = this.Vpc,
            SecurityGroupName = $"{AppConstants.StackPrefix}-Lambda-Sg",
            Description = "Security group for Lambda functions in the VPC",
            AllowAllOutbound = true,
        });

        _ = this.Vpc.AddGatewayEndpoint("DynamoDbEndpoint", new GatewayVpcEndpointOptions { Service = GatewayVpcEndpointAwsService.DYNAMODB });

        var cloudWatchLogsEndpointSecurityGroup = new SecurityGroup(this, "CloudWatchLogsEndpointSecurityGroup", new SecurityGroupProps
        {
            Vpc = this.Vpc,
            SecurityGroupName = $"{AppConstants.StackPrefix}-Logs-Endpoint-Sg",
            Description = "Security group for the CloudWatch Logs interface endpoint",
            AllowAllOutbound = false,
        });
        cloudWatchLogsEndpointSecurityGroup.AddIngressRule(this.LambdaSecurityGroup, Port.Tcp(443), "Allow Lambda functions to reach CloudWatch Logs over HTTPS");

        _ = this.Vpc.AddInterfaceEndpoint("CloudWatchLogsEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.CLOUDWATCH_LOGS,
            PrivateDnsEnabled = true,
            SecurityGroups = [cloudWatchLogsEndpointSecurityGroup],
        });

        _ = new CfnOutput(this, "VpcId", new CfnOutputProps { Description = "VPC identifier for the serverless workload", Value = this.Vpc.VpcId });
    }

    public ISecurityGroup LambdaSecurityGroup { get; }

    public IVpc Vpc { get; }
}
