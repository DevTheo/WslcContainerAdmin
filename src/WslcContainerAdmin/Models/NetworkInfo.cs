namespace WslcContainerAdmin.Models;

/// <summary>One row of `wslc network ls --format json`. <see cref="CreatedAt"/> is kept as the CLI's raw string.</summary>
public sealed record NetworkInfo(
    string Id,
    string Name,
    string Driver,
    string Scope,
    bool IPv4,
    bool IPv6,
    bool Internal,
    string CreatedAt);
