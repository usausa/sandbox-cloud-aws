using Amazon.CDK.AWS.EC2;

using Constructs;

namespace TenantApi.IaC.Constructs;

/// <summary>
/// VPC, Subnets, Security Groups and VPC Endpoints.
/// All workloads run in a single private isolated subnet (1AZ) with no
/// internet egress. AWS service traffic flows through VPC endpoints.
/// </summary>
internal sealed class NetworkConstruct : Construct
{
    public Vpc Vpc { get; }

    public SecurityGroup EcsTaskSecurityGroup { get; }

    public SecurityGroup AuroraSecurityGroup { get; }

    public SecurityGroup VpcEndpointSecurityGroup { get; }

    public SecurityGroup VpcLinkSecurityGroup { get; }

    public NetworkConstruct(Construct scope, string id)
        : base(scope, id)
    {
        Vpc = new Vpc(this, "Vpc", new VpcProps
        {
            IpAddresses = IpAddresses.Cidr(Constants.VpcCidr),
            MaxAzs = Constants.VpcMaxAzs,
            AvailabilityZones = [Constants.AvailabilityZone],
            NatGateways = 0,
            SubnetConfiguration =
            [
                new SubnetConfiguration
                {
                    Name = "private",
                    SubnetType = SubnetType.PRIVATE_ISOLATED,
                    CidrMask = 24,
                },
            ],
        });

        EcsTaskSecurityGroup = new SecurityGroup(this, "EcsTaskSg", new SecurityGroupProps
        {
            Vpc = Vpc,
            Description = "ECS task SG",
            AllowAllOutbound = false,
        });

        AuroraSecurityGroup = new SecurityGroup(this, "AuroraSg", new SecurityGroupProps
        {
            Vpc = Vpc,
            Description = "Aurora SG",
            AllowAllOutbound = true,
        });

        VpcEndpointSecurityGroup = new SecurityGroup(this, "VpcEndpointSg", new SecurityGroupProps
        {
            Vpc = Vpc,
            Description = "Interface VPC endpoint SG",
            AllowAllOutbound = true,
        });

        VpcLinkSecurityGroup = new SecurityGroup(this, "VpcLinkSg", new SecurityGroupProps
        {
            Vpc = Vpc,
            Description = "API Gateway VPC Link SG",
            AllowAllOutbound = true,
        });

        // Aurora <- ECS
        AuroraSecurityGroup.AddIngressRule(EcsTaskSecurityGroup, Port.Tcp(Constants.DbPort), "ECS to Aurora");

        // ECS -> Aurora
        EcsTaskSecurityGroup.AddEgressRule(AuroraSecurityGroup, Port.Tcp(Constants.DbPort), "ECS to Aurora");

        // ECS -> VPC Endpoints (HTTPS)
        EcsTaskSecurityGroup.AddEgressRule(VpcEndpointSecurityGroup, Port.Tcp(443), "ECS to VPC Endpoints");
        VpcEndpointSecurityGroup.AddIngressRule(EcsTaskSecurityGroup, Port.Tcp(443), "ECS to VPC Endpoints");

        // VPC Link -> ECS Task
        EcsTaskSecurityGroup.AddIngressRule(VpcLinkSecurityGroup, Port.Tcp(Constants.ContainerPort), "VPC Link to ECS task");

        // Interface endpoints
        var endpointSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED };

        _ = Vpc.AddInterfaceEndpoint("EcrApiEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.ECR,
            PrivateDnsEnabled = true,
            SecurityGroups = [VpcEndpointSecurityGroup],
            Subnets = endpointSubnets,
        });

        _ = Vpc.AddInterfaceEndpoint("EcrDkrEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.ECR_DOCKER,
            PrivateDnsEnabled = true,
            SecurityGroups = [VpcEndpointSecurityGroup],
            Subnets = endpointSubnets,
        });

        _ = Vpc.AddInterfaceEndpoint("LogsEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.CLOUDWATCH_LOGS,
            PrivateDnsEnabled = true,
            SecurityGroups = [VpcEndpointSecurityGroup],
            Subnets = endpointSubnets,
        });

        // Gateway endpoint (free) for S3 (used by ECR layer download)
        _ = Vpc.AddGatewayEndpoint("S3Endpoint", new GatewayVpcEndpointOptions
        {
            Service = GatewayVpcEndpointAwsService.S3,
            Subnets = [endpointSubnets],
        });
    }
}
