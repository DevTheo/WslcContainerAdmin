using System.Text.Json;
using Microsoft.Win32;
using WslcContainerAdmin.Models;

namespace WslcContainerAdmin.Services;

public interface IStartupPreferencesService
{
    StartupPreferences Current { get; }
    void SetContainerAutoStart(string containerId, bool enabled);
    void SetLaunchAtSignIn(bool enabled);
}

public sealed class StartupPreferencesService : IStartupPreferencesService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "WslcContainerAdmin";
    private readonly string _settingsPath;
    private readonly HashSet<string> _autoStartContainerIds;

    public StartupPreferencesService()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WslcContainerAdmin");
        Directory.CreateDirectory(directory);
        _settingsPath = Path.Combine(directory, "settings.json");
        var stored = File.Exists(_settingsPath)
            ? JsonSerializer.Deserialize<StoredPreferences>(File.ReadAllText(_settingsPath))
            : null;
        _autoStartContainerIds = stored?.AutoStartContainerIds?.ToHashSet(StringComparer.Ordinal) ?? [];
        Current = new StartupPreferences(stored?.LaunchAtSignIn ?? false, _autoStartContainerIds);
    }

    public StartupPreferences Current { get; private set; }

    public void SetContainerAutoStart(string containerId, bool enabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerId);
        if (enabled) _autoStartContainerIds.Add(containerId);
        else _autoStartContainerIds.Remove(containerId);
        Save(Current.LaunchAtSignIn);
    }

    public void SetLaunchAtSignIn(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
        {
            var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot determine application executable path.");
            key.SetValue(RunValueName, $"\"{executable}\"");
        }
        else
        {
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
        Save(enabled);
    }

    private void Save(bool launchAtSignIn)
    {
        Current = new StartupPreferences(launchAtSignIn, _autoStartContainerIds);
        var json = JsonSerializer.Serialize(new StoredPreferences(launchAtSignIn, _autoStartContainerIds.Order(StringComparer.Ordinal).ToArray()), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }

    private sealed record StoredPreferences(bool LaunchAtSignIn, string[]? AutoStartContainerIds);
}
