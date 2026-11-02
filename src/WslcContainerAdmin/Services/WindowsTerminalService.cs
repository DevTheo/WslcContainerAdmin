using System.Diagnostics;

namespace WslcContainerAdmin.Services;

public sealed class WindowsTerminalService : IContainerTerminalService
{
    public void OpenShell(string containerId)
    {
        if (string.IsNullOrWhiteSpace(containerId))
        {
            throw new ArgumentException("A container ID is required.", nameof(containerId));
        }

        var startInfo = new ProcessStartInfo("wt.exe")
        {
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("-w");
        startInfo.ArgumentList.Add("new");
        startInfo.ArgumentList.Add("new-tab");
        startInfo.ArgumentList.Add("--title");
        startInfo.ArgumentList.Add($"WSLc container {containerId.Trim()}");
        startInfo.ArgumentList.Add("wslc");
        startInfo.ArgumentList.Add("container");
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add("--interactive");
        startInfo.ArgumentList.Add("--tty");
        startInfo.ArgumentList.Add(containerId.Trim());
        startInfo.ArgumentList.Add("/bin/sh");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to open Windows Terminal.");
    }
}
