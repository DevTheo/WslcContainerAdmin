using System.Text.Json;
using WslcContainerAdmin.Models;

namespace WslcContainerAdmin.Services;

public sealed class WslcResourceDetailsService(IWslcCli cli, IWslcContainerService containers) : IResourceDetailsService
{
    public async Task<VolumeDetails> GetVolumeDetailsAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var result = await RunCheckedAsync(["volume", "inspect", "--format", "json", name.Trim()], "volume inspect", cancellationToken);
        using var document = JsonDocument.Parse(result.StandardOutput);
        var volume = First(document.RootElement);
        var users = new List<ContainerInfo>();
        foreach (var container in await containers.ListAsync(includeStopped: true, cancellationToken))
        {
            var inspect = await containers.InspectAsync(container.Id, cancellationToken);
            using var containerDocument = JsonDocument.Parse(inspect);
            foreach (var inspected in Elements(containerDocument.RootElement))
            {
                if (!inspected.TryGetProperty("Mounts", out var mounts) || mounts.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidDataException("wslc container inspect output did not contain a Mounts array.");
                }

                if (mounts.EnumerateArray().Any(mount => Text(mount, "Name") == name))
                {
                    users.Add(container);
                    break;
                }
            }
        }

        return new VolumeDetails(
            Text(volume, "Name"),
            Text(volume, "CreatedAt"),
            Text(volume, "Mountpoint"),
            Text(volume, "Driver"),
            Text(volume, "Scope"),
            users);
    }

    public async Task<NetworkDetails> GetNetworkDetailsAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var result = await RunCheckedAsync(["network", "inspect", "--format", "json", name.Trim()], "network inspect", cancellationToken);
        using var document = JsonDocument.Parse(result.StandardOutput);
        var network = First(document.RootElement);
        var attached = await containers.ListAsync(includeStopped: true, cancellationToken);
        if (!network.TryGetProperty("Containers", out var endpoints) || endpoints.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("wslc network inspect output did not contain a Containers object.");
        }
        var members = new List<NetworkEndpoint>();
        foreach (var endpoint in endpoints.EnumerateObject())
        {
            var container = attached.FirstOrDefault(item =>
                item.Id.StartsWith(endpoint.Name, StringComparison.OrdinalIgnoreCase) ||
                endpoint.Name.StartsWith(item.Id, StringComparison.OrdinalIgnoreCase));
            members.Add(new NetworkEndpoint(
                endpoint.Name,
                Text(endpoint.Value, "Name").TrimStart('/'),
                Text(endpoint.Value, "EndpointID"),
                Text(endpoint.Value, "MacAddress"),
                Text(endpoint.Value, "IPv4Address"),
                Text(endpoint.Value, "IPv6Address"),
                container));
        }

        var ipamDriver = string.Empty;
        var ipamConfigs = new List<NetworkIpamConfig>();
        if (network.TryGetProperty("IPAM", out var ipam) && ipam.ValueKind == JsonValueKind.Object)
        {
            ipamDriver = Text(ipam, "Driver");
            if (ipam.TryGetProperty("Config", out var configs) && configs.ValueKind == JsonValueKind.Array)
            {
                ipamConfigs.AddRange(configs.EnumerateArray().Select(config => new NetworkIpamConfig(
                    Text(config, "Subnet"), Text(config, "Gateway"), Text(config, "IPRange"))));
            }
        }

        var options = new List<KeyValuePair<string, string>>();
        if (network.TryGetProperty("Options", out var optionObject) && optionObject.ValueKind == JsonValueKind.Object)
        {
            options.AddRange(optionObject.EnumerateObject().Select(option => new KeyValuePair<string, string>(option.Name, Value(option.Value))));
        }

        return new NetworkDetails(
            Text(network, "Id"), Text(network, "Name"), Text(network, "Driver"), Text(network, "Scope"),
            Flag(network, "Attachable"), Flag(network, "Internal"), Flag(network, "EnableIPv4"), Flag(network, "EnableIPv6"),
            Text(network, "Created"), ipamDriver, ipamConfigs, options, members);
    }

    public async Task DisconnectNetworkAsync(string networkName, string containerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(networkName);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerId);
        await RunCheckedAsync(["network", "disconnect", networkName.Trim(), containerId.Trim()], "network disconnect", cancellationToken);
    }

    private async Task<WslcCliResult> RunCheckedAsync(IReadOnlyList<string> arguments, string command, CancellationToken cancellationToken)
    {
        var result = await cli.RunAsync(arguments, cancellationToken);
        if (result.ExitCode != 0)
        {
            var detail = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError;
            throw new InvalidOperationException($"wslc {command} failed (exit {result.ExitCode}): {detail.Trim()}");
        }
        return result;
    }

    private static JsonElement First(JsonElement root) => root.ValueKind == JsonValueKind.Array ? root[0] : root;

    private static IEnumerable<JsonElement> Elements(JsonElement root) =>
        root.ValueKind == JsonValueKind.Array ? root.EnumerateArray().ToArray() : [root];

    private static string Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    private static bool Flag(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True ||
        string.Equals(Text(element, name), "true", StringComparison.OrdinalIgnoreCase);

    private static string Value(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
        _ => value.ToString(),
    };
}
