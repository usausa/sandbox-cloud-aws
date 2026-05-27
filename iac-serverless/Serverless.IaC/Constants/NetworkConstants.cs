// <copyright file="NetworkConstants.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Serverless.IaC.Constants;

internal static class NetworkConstants
{
    public const int MaxAzs = 2;

    public const int NatGateways = 1;

    public const int PrivateSubnetCidrMask = 24;

    public const int PublicSubnetCidrMask = 24;

    public const string VpcCidr = "10.0.0.0/16";
}
