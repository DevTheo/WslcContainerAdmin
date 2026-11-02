namespace WslcContainerAdmin.Models;

/// <summary>One row from `wslc image list --format json`.</summary>
public sealed record ImageInfo(
    string Id,
    string Repository,
    string Tag,
    string Size,
    string CreatedSince,
    string Digest,
    string Containers)
{
    public bool? IsUnused => int.TryParse(Containers, out var count) ? count == 0 : null;

    public string UsageStatus => IsUnused switch
    {
        true => "Unused",
        false => "In use",
        null => "Unknown",
    };
}
