using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WslcContainerAdmin.Models;
using WslcContainerAdmin.Services;

namespace WslcContainerAdmin.ViewModels;

public partial class ContainerDetailViewModel(
    IWslcContainerService containers,
    INavigationService navigation,
    IServiceProvider services,
    IConfirmationService confirmation) : ViewModelBase
{
    private CancellationTokenSource? _lifetime;
    private ViewModelBase? _returnViewModel;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenTerminalCommand))]
    private ContainerInfo? _container;

    [ObservableProperty]
    private string _inspectJson = string.Empty;

    [ObservableProperty]
    private string _statsJson = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    public ObservableCollection<string> LogLines { get; } = [];

    public void SetContainer(ContainerInfo container)
    {
        Container = container;
        _returnViewModel = navigation.Current;
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        var target = _returnViewModel ?? services.GetRequiredService<ContainersViewModel>();
        await navigation.NavigateAsync(target);
    }

    [RelayCommand(CanExecute = nameof(CanOpenTerminal))]
    private void OpenTerminal()
    {
        if (Container is null)
        {
            return;
        }

        try
        {
            services.GetRequiredService<IContainerTerminalService>().OpenShell(Container.Id);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private bool CanOpenTerminal() =>
        string.Equals(Container?.State, "running", StringComparison.OrdinalIgnoreCase);
    public override async Task OnNavigatedToAsync()
    {
        if (Container is null)
        {
            return;
        }

        _lifetime = new CancellationTokenSource();
        await RefreshAsync();
        _ = PollStatsAsync(_lifetime.Token);
        _ = FollowLogsAsync(_lifetime.Token);
    }

    public override void OnNavigatedFrom() => _lifetime?.Cancel();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (Container is null)
        {
            return;
        }

        ErrorMessage = null;
        try
        {
            var results = await Task.WhenAll(
                containers.InspectAsync(Container.Id),
                containers.GetStatsAsync(Container.Id));
            InspectJson = results[0];
            StatsJson = results[1];
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        if (Container is not null)
        {
            await RunActionAsync(() => containers.StartAsync(Container.Id));
        }
    }

    [RelayCommand]
    private async Task StopAsync()
    {
        if (Container is not null)
        {
            await RunActionAsync(() => containers.StopAsync(Container.Id));
        }
    }

    [RelayCommand]
    private async Task RemoveAsync()
    {
        if (Container is not { } selected ||
            !await confirmation.ConfirmAsync("Remove container", $"Permanently remove '{selected.Name}'?"))
        {
            return;
        }

        try
        {
            await containers.RemoveAsync(selected.Id);
            var list = services.GetRequiredService<ContainersViewModel>();
            await navigation.NavigateAsync(list);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task RunActionAsync(Func<Task> action)
    {
        ErrorMessage = null;
        try
        {
            await action();
            var id = Container!.Id;
            var refreshed = await containers.ListAsync(includeStopped: true);
            Container = refreshed.FirstOrDefault(item => item.Id == id) ?? Container;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task PollStatsAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var stats = await containers.GetStatsAsync(Container!.Id, cancellationToken);
                await Dispatcher.UIThread.InvokeAsync(() => StatsJson = stats);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ErrorMessage = ex.Message);
        }
    }

    private async Task FollowLogsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await containers.FollowLogsAsync(Container!.Id, async line =>
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LogLines.Add(line);
                    if (LogLines.Count > 1000)
                    {
                        LogLines.RemoveAt(0);
                    }
                });
            }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ErrorMessage = ex.Message);
        }
    }
}
