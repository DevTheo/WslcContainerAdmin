namespace WslcContainerAdmin.Services;

/// <summary>Result of one `wslc` invocation.</summary>
public sealed record WslcCliResult(int ExitCode, string StandardOutput, string StandardError);

/// <summary>Runs the installed <c>wslc.exe</c> CLI using argument-safe process invocation.</summary>
public interface IWslcCli
{
    Task<WslcCliResult> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken = default);
    Task<WslcCliResult> RunStreamingAsync(IReadOnlyList<string> arguments, Func<string, Task> onStandardOutputLine, CancellationToken cancellationToken = default);
}
