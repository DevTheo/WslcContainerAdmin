using System.Globalization;
using WslcContainerAdmin.Models;

namespace WslcContainerAdmin.Services;

public sealed class WslcContainerService(IWslcCli cli) : IWslcContainerService
{
    public async Task<IReadOnlyList<ContainerInfo>> ListAsync(bool includeStopped, CancellationToken cancellationToken = default)
    {
        var args = new List<string> { "container", "list" };
        if (includeStopped)
        {
            args.Add("--all");
        }
        args.AddRange(["--format", "json"]);

        var result = await RunCheckedAsync(args, cancellationToken);
        return WslcJson.ReadLines(result.StandardOutput).Select(row => new ContainerInfo(
            Id: WslcJson.Text(row, "ID"),
            Name: WslcJson.Text(row, "Names"),
            Image: WslcJson.Text(row, "Image"),
            Command: WslcJson.Text(row, "Command"),
            CreatedAt: WslcJson.Text(row, "CreatedAt"),
            Status: WslcJson.Text(row, "Status"),
            State: WslcJson.Text(row, "State"),
            Ports: WslcJson.Text(row, "Ports"),
            Networks: WslcJson.Text(row, "Networks"),
            Platform: WslcJson.Text(row, "Platform"),
            Labels: WslcJson.Text(row, "Labels"))).ToList();
    }

    public async Task RunAsync(ContainerRunOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.Image))
        {
            throw new ArgumentException("An image is required.", nameof(options));
        }

        var args = new List<string> { "container", "run" };
        if (options.RunInBackground)
        {
            args.Add("--detach");
        }
        AddOption(args, "--name", options.Name);
        if (options.RemoveOnExit)
        {
            args.Add("--rm");
        }
        if (options.KeepStdinOpen)
        {
            args.Add("--interactive");
        }
        if (options.PassAllGpus)
        {
            args.AddRange(["--gpus", "all"]);
        }
        if (options.StopTimeoutSeconds is int timeoutSeconds)
        {
            args.AddRange(["--stop-timeout", timeoutSeconds.ToString(CultureInfo.InvariantCulture)]);
        }
        AddOption(args, "--entrypoint", options.Entrypoint);
        AddOption(args, "--workdir", options.WorkingDirectory);
        AddOption(args, "--user", options.User);
        if (options.AllocateTty)
        {
            args.Add("--tty");
        }
        AddOption(args, "--network", options.Network);
        AddOption(args, "--hostname", options.Hostname);
        AddOption(args, "--domainname", options.DomainName);
        AddOption(args, "--ip", options.Ipv4Address);
        AddOptions(args, "--dns", options.DnsServers ?? []);
        AddOption(args, "--env-file", options.EnvironmentFile);
        AddOptions(args, "--label", options.Labels ?? []);
        AddOption(args, "--memory", options.MemoryLimit);
        AddOption(args, "--cpus", options.CpuLimit);
        AddOption(args, "--shm-size", options.SharedMemorySize);
        var publishedPorts = options.PublishedPorts.Select(mapping => $"{mapping.HostPort}:{mapping.ContainerPort}");
        var volumeMounts = options.VolumeMounts.Select(mount => $"{mount.VolumeName}:{mount.ContainerPath}");
        AddOptions(args, "--publish", publishedPorts);
        AddOptions(args, "--volume", volumeMounts);
        AddOptions(args, "--env", options.EnvironmentVariables);
        args.Add(options.Image.Trim());
        AddOption(args, null, options.Command);
        args.AddRange(options.Arguments.Where(value => !string.IsNullOrWhiteSpace(value)));
        await RunCheckedAsync(args, cancellationToken);
    }

    public async Task StartAsync(string id, CancellationToken cancellationToken = default) =>
        await RunCheckedAsync(["container", "start", RequiredId(id)], cancellationToken);

    public async Task StopAsync(string id, CancellationToken cancellationToken = default) =>
        await RunCheckedAsync(["container", "stop", RequiredId(id)], cancellationToken);

    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default) =>
        await RunCheckedAsync(["container", "remove", RequiredId(id)], cancellationToken);

    public async Task<string> InspectAsync(string id, CancellationToken cancellationToken = default) =>
        (await RunCheckedAsync(["container", "inspect", RequiredId(id)], cancellationToken)).StandardOutput;

    public async Task<string> GetLogsAsync(string id, CancellationToken cancellationToken = default) =>
        (await RunCheckedAsync(["container", "logs", "--tail", "500", RequiredId(id)], cancellationToken)).StandardOutput;

    public async Task<string> GetStatsAsync(string id, CancellationToken cancellationToken = default) =>
        (await RunCheckedAsync(["container", "stats", "--format", "json", RequiredId(id)], cancellationToken)).StandardOutput;

    public async Task FollowLogsAsync(string id, Func<string, Task> onLine, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onLine);
        var result = await cli.RunStreamingAsync(
            ["container", "logs", "--follow", "--tail", "100", RequiredId(id)],
            onLine,
            cancellationToken);
        EnsureSuccess(result, "container logs");
    }

    private async Task<WslcCliResult> RunCheckedAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var result = await cli.RunAsync(arguments, cancellationToken);
        EnsureSuccess(result, $"{arguments[0]} {arguments[1]}");
        return result;
    }

    private static void EnsureSuccess(WslcCliResult result, string command)
    {
        if (result.ExitCode != 0)
        {
            var detail = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError;
            throw new InvalidOperationException($"wslc {command} failed (exit {result.ExitCode}): {detail.Trim()}");
        }
    }

    private static void AddOptions(List<string> arguments, string option, IEnumerable<string> values)
    {
        foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            arguments.Add(option);
            arguments.Add(value.Trim());
        }
    }

    private static void AddOption(List<string> arguments, string? option, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (option is not null)
            {
                arguments.Add(option);
            }
            arguments.Add(value.Trim());
        }
    }

    private static string RequiredId(string id) =>
        !string.IsNullOrWhiteSpace(id) ? id.Trim() : throw new ArgumentException("A container ID is required.", nameof(id));
}
