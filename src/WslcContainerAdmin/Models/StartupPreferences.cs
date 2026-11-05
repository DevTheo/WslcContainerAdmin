namespace WslcContainerAdmin.Models;

public sealed record StartupPreferences(bool LaunchAtSignIn, IReadOnlySet<string> AutoStartContainerIds);
