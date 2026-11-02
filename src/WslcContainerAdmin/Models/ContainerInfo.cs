namespace WslcContainerAdmin.Models;

/// <summary>One row from `wslc container list --all --format json`.</summary>
public sealed record ContainerInfo(
    string Id,
    string Name,
    string Image,
    string Command,
    string CreatedAt,
    string Status,
    string State,
    string Ports,
    string Networks,
    string Platform,
    string Labels);
