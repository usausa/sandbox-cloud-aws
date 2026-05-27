namespace Tenant.IaC.Stacks;

using System;
using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;
using Tenant.IaC.Constants;

internal sealed class NetworkStack : Stack
{
    public NetworkStack(Construct scope, string id, StackProps props)
        : base(scope, id, props)
    {
        if (NetworkConstants.AvailabilityZones.Length != NetworkConstants.MaxAzs)
        {
            throw new InvalidOperationException("Configured availability zones do not match the expected AZ count.");
        }

        var vpc = new Vpc(this, NetworkConstants.VpcConstructId, new VpcProps
        {
            VpcName = NetworkConstants.VpcName,
            IpAddresses = IpAddresses.Cidr(NetworkConstants.VpcCidr),
            AvailabilityZones = NetworkConstants.AvailabilityZones,
            NatGateways = 0,
            SubnetConfiguration =
            [
                new SubnetConfiguration
                {
                    Name = NetworkConstants.PublicSubnetGroupName,
                    SubnetType = SubnetType.PUBLIC,
                    CidrMask = NetworkConstants.SubnetCidrMask,
                },
                new SubnetConfiguration
                {
                    Name = NetworkConstants.IsolatedSubnetGroupName,
                    SubnetType = SubnetType.PRIVATE_ISOLATED,
                    CidrMask = NetworkConstants.SubnetCidrMask,
                },
            ],
        });

        ConfigureSubnets(vpc.PublicSubnets, NetworkConstants.PublicSubnetCidrs);
        ConfigureSubnets(vpc.IsolatedSubnets, NetworkConstants.IsolatedSubnetCidrs);

        Vpc = vpc;
        AlbSg = new SecurityGroup(this, NetworkConstants.AlbSecurityGroupConstructId, new SecurityGroupProps
        {
            Vpc = Vpc,
            AllowAllOutbound = true,
            Description = NetworkConstants.PublicIngressDescription,
        });
        EcsSg = new SecurityGroup(this, NetworkConstants.EcsSecurityGroupConstructId, new SecurityGroupProps
        {
            Vpc = Vpc,
            AllowAllOutbound = true,
            Description = NetworkConstants.EcsIngressDescription,
        });
        AuroraSg = new SecurityGroup(this, NetworkConstants.AuroraSecurityGroupConstructId, new SecurityGroupProps
        {
            Vpc = Vpc,
            AllowAllOutbound = true,
            Description = NetworkConstants.AuroraIngressDescription,
        });

        AlbSg.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(NetworkConstants.AlbPort), NetworkConstants.PublicIngressDescription);
        EcsSg.AddIngressRule(AlbSg, Port.Tcp(NetworkConstants.ContainerPort), NetworkConstants.EcsIngressDescription);
        AuroraSg.AddIngressRule(EcsSg, Port.Tcp(NetworkConstants.AuroraPort), NetworkConstants.AuroraIngressDescription);

        _ = new CfnOutput(this, NetworkConstants.VpcOutputId, new CfnOutputProps
        {
            Value = Vpc.VpcId,
        });
    }

    public IVpc Vpc { get; }

    public ISecurityGroup AlbSg { get; }

    public ISecurityGroup EcsSg { get; }

    public ISecurityGroup AuroraSg { get; }

    private static void ConfigureSubnets(IEnumerable<ISubnet> subnets, string[] cidrBlocks)
    {
        var index = 0;
        foreach (var subnet in subnets)
        {
            if (index >= cidrBlocks.Length || index >= NetworkConstants.AvailabilityZones.Length)
            {
                throw new InvalidOperationException("Subnet configuration count does not match the configured availability zones.");
            }

            if (subnet.Node.DefaultChild is not CfnSubnet cfnSubnet)
            {
                throw new InvalidOperationException("Expected subnet default child to be a CfnSubnet.");
            }

            cfnSubnet.CidrBlock = cidrBlocks[index];
            cfnSubnet.AvailabilityZone = NetworkConstants.AvailabilityZones[index];
            index++;
        }

        if (index != cidrBlocks.Length)
        {
            throw new InvalidOperationException("Configured subnet count does not match the generated subnet count.");
        }
    }
}
