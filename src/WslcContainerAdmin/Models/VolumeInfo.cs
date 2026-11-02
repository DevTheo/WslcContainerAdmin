namespace WslcContainerAdmin.Models;

/// <summary>One row of `wslc volume ls --format json`.</summary>
public sealed record VolumeInfo(string Name, string Driver, string Scope, string Mountpoint, bool? IsUnused = null)
{
    public string UsageStatus => IsUnused switch
    {
        true => "Unused",
        false => "In use",
        null => "Unknown",
    };
}
