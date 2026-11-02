using System.Diagnostics;

namespace WslcContainerAdmin.Services;

/// <summary>Process boundary that launches the installed <c>wslc.exe</c>; tests of this adapter require integration-test treatment.</summary>
public sealed class WslcCli : IWslcCli
{
    public async Task<WslcCliResult> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        using var process = Start(arguments);
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await WaitForExitAsync(process, cancellationToken);
        return new WslcCliResult(process.ExitCode, await standardOutput, await standardError);
    }

    public async Task<WslcCliResult> RunStreamingAsync(
        IReadOnlyList<string> arguments,
        Func<string, Task> onStandardOutputLine,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(onStandardOutputLine);

        using var process = Start(arguments);
        var standardError = process.StandardError.ReadToEndAsync();
        try
        {
            while (await process.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
            {
                await onStandardOutputLine(line);
            }

            await process.WaitForExitAsync(cancellationToken);
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
            await standardError;
            throw;
        }

        return new WslcCliResult(process.ExitCode, string.Empty, await standardError);
    }

    private static Process Start(IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo("wslc")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start wslc.");
    }

    private static async Task WaitForExitAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
            throw;
        }
    }
}
