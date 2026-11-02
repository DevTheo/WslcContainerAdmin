using System.Text.Json;
using WslcContainerAdmin.Models;

namespace WslcContainerAdmin.Services;

/// <summary>
/// Lists volumes and networks by parsing `wslc &lt;noun&gt; ls --format json`, which prints one JSON object per line.
/// </summary>
public sealed class WslcInventoryService(IWslcCli cli) : IWslcInventoryService
{
    public async Task<IReadOnlyList<VolumeInfo>> ListVolumesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await ListJsonLinesAsync("volume", cancellationToken);
        var usedVolumes = await ListUsedVolumeNamesAsync(cancellationToken);
        return rows.Select(row =>
        {
            var name = WslcJson.Text(row, "Name");
            return new VolumeInfo(name, WslcJson.Text(row, "Driver"), WslcJson.Text(row, "Scope"), WslcJson.Text(row, "Mountpoint"), !usedVolumes.Contains(name));
        }).ToList();
    }

    public async Task CreateVolumeAsync(string name, string driver = "guest", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(driver))
        {
            throw new ArgumentException("A volume name and driver are required.");
        }
        await RunMutationAsync(["volume", "create", "--driver", driver.Trim(), name.Trim()], "volume create", cancellationToken);
    }

    public async Task RemoveVolumeAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A volume name is required.", nameof(name));
        }
        await RunMutationAsync(["volume", "remove", name.Trim()], "volume remove", cancellationToken);
    }

    private async Task<HashSet<string>> ListUsedVolumeNamesAsync(CancellationToken cancellationToken)
    {
        var result = await cli.RunAsync(["container", "list", "--all", "--format", "json"], cancellationToken);
        EnsureSuccess(result, "container list");
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in WslcJson.ReadLines(result.StandardOutput))
        {
            var id = WslcJson.Text(row, "ID");
            var inspect = await cli.RunAsync(["container", "inspect", id], cancellationToken);
            EnsureSuccess(inspect, "container inspect");
            using var document = JsonDocument.Parse(inspect.StandardOutput);
            var containers = document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.EnumerateArray().ToArray()
                : [document.RootElement];
            foreach (var container in containers)
            {
                if (!container.TryGetProperty("Mounts", out var mounts) || mounts.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidDataException("wslc container inspect output did not contain a Mounts array.");
                }
                foreach (var mount in mounts.EnumerateArray())
                {
                    var name = WslcJson.Text(mount, "Name");
                    if (name.Length != 0)
                    {
                        used.Add(name);
                    }
                }
            }
        }
        return used;
    }

    private async Task RunMutationAsync(IReadOnlyList<string> arguments, string command, CancellationToken cancellationToken)
    {
        var result = await cli.RunAsync(arguments, cancellationToken);
        EnsureSuccess(result, command);
    }

    private static void EnsureSuccess(WslcCliResult result, string command)
    {
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"wslc {command} failed (exit {result.ExitCode}): {result.StandardError.Trim()}");
        }
    }

    public async Task<IReadOnlyList<NetworkInfo>> ListNetworksAsync(CancellationToken cancellationToken = default)
    {
        var rows = await ListJsonLinesAsync("network", cancellationToken);

        return rows.Select(row => new NetworkInfo(
            Id: WslcJson.Text(row, "ID"),
            Name: WslcJson.Text(row, "Name"),
            Driver: WslcJson.Text(row, "Driver"),
            Scope: WslcJson.Text(row, "Scope"),
            IPv4: Flag(row, "IPv4"),
            IPv6: Flag(row, "IPv6"),
            Internal: Flag(row, "Internal"),
            CreatedAt: WslcJson.Text(row, "CreatedAt"))).ToList();
    }

    private async Task<List<JsonElement>> ListJsonLinesAsync(string noun, CancellationToken cancellationToken)
    {
        var result = await cli.RunAsync([noun, "ls", "--format", "json"], cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"wslc {noun} ls failed (exit {result.ExitCode}): {result.StandardError.Trim()}");
        }

        return WslcJson.ReadLines(result.StandardOutput);
    }

    // The CLI emits booleans as strings ("true"/"false").
    private static bool Flag(JsonElement row, string property) =>
        string.Equals(WslcJson.Text(row, property), "true", StringComparison.OrdinalIgnoreCase);
}
