using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WslcContainerAdmin.Services;
using WslcContainerAdmin.ViewModels;

namespace WslcContainerAdmin;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        if (TrayIcon.GetIcons(this)?.FirstOrDefault() is { } trayIcon)
        {
            trayIcon.Command = new RelayCommand(RestoreMainWindow);
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Services = ConfigureServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnTrayOpenClicked(object? sender, EventArgs e) => RestoreMainWindow();

    private void RestoreMainWindow()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow window })
        {
            window.RestoreFromTray();
        }
    }

    private void OnTrayExitClicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private static ServiceProvider ConfigureServices() =>
        new ServiceCollection().AddWslcContainerAdminServices().BuildServiceProvider();
}
