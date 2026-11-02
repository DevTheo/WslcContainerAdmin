namespace WslcContainerAdmin.Models;

public sealed record VolumeDetails(
    string Name,
    string CreatedAt,
    string Mountpoint,
    string Driver,
    string Scope,
    IReadOnlyList<ContainerInfo> UsingContainers)
{
    public bool IsUnused => UsingContainers.Count == 0;
}

public sealed record NetworkIpamConfig(string Subnet, string Gateway, string IpRange)
{
    public bool HasIpRange => !string.IsNullOrWhiteSpace(IpRange);
}

public sealed record NetworkEndpoint(
    string ContainerId,
    string Name,
    string EndpointId,
    string MacAddress,
    string IPv4Address,
    string IPv6Address,
    ContainerInfo? Container)
{
    public bool CanOpenDetails => Container is not null;
}
public sealed record NetworkDetails(
    string Id,
    string Name,
    string Driver,
    string Scope,
    bool Attachable,
    bool Internal,
    bool IPv4Enabled,
    bool IPv6Enabled,
    string CreatedAt,
    string IpamDriver,
    IReadOnlyList<NetworkIpamConfig> IpamConfigs,
    IReadOnlyList<KeyValuePair<string, string>> Options,
    IReadOnlyList<NetworkEndpoint> Containers);
