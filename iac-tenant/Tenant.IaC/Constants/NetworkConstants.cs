namespace Tenant.IaC.Constants;

internal static class NetworkConstants
{
    public const string VpcConstructId = "TenantVpc";
    public const string VpcName = "tenant-vpc";
    public const string VpcCidr = "10.0.0.0/16";
    public const int MaxAzs = 2;
    public const int SubnetCidrMask = 24;
    public const int ContainerPort = 8080;
    public const int AlbPort = 80;
    public const int AuroraPort = 5432;
    public const string PublicSubnetGroupName = "public";
    public const string IsolatedSubnetGroupName = "private-isolated";
    public const string AlbSecurityGroupConstructId = "AlbSecurityGroup";
    public const string EcsSecurityGroupConstructId = "EcsSecurityGroup";
    public const string AuroraSecurityGroupConstructId = "AuroraSecurityGroup";
    public const string VpcOutputId = "VpcId";
    public const string PublicIngressDescription = "Allow HTTP from the internet";
    public const string EcsIngressDescription = "Allow HTTP from the ALB";
    public const string AuroraIngressDescription = "Allow PostgreSQL from ECS";

    public static readonly string[] AvailabilityZones = ["ap-northeast-1a", "ap-northeast-1c"];

    public static readonly string[] PublicSubnetCidrs = ["10.0.0.0/24", "10.0.1.0/24"];

    public static readonly string[] IsolatedSubnetCidrs = ["10.0.10.0/24", "10.0.11.0/24"];
}
