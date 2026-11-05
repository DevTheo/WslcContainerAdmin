using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WslcContainerAdmin.Models;
using WslcContainerAdmin.Services;

namespace WslcContainerAdmin.ViewModels;

public partial class ContainersViewModel(
    IWslcContainerService containers,
    INavigationService navigation,
    IServiceProvider services,
    IConfirmationService confirmation,
    IStartupPreferencesService startupPreferences) : ViewModelBase
{
    public ObservableCollection<ContainerInfo> Items { get; } = [];

    [ObservableProperty]
    private ContainerInfo? _selectedContainer;

    [ObservableProperty]
    private bool _includeStopped = true;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _launchAtSignIn = startupPreferences.Current.LaunchAtSignIn;

    private bool _startupContainersProcessed;

    public bool SelectedAutoStartEnabled => SelectedContainer is not null && startupPreferences.Current.AutoStartContainerIds.Contains(SelectedContainer.Id);

    partial void OnSelectedContainerChanged(ContainerInfo? value) => OnPropertyChanged(nameof(SelectedAutoStartEnabled));

    partial void OnLaunchAtSignInChanged(bool value)
    {
        try { startupPreferences.SetLaunchAtSignIn(value); }
        catch (Exception ex) { ErrorMessage = ex.Message; LaunchAtSignIn = !value; }
    }

    [RelayCommand]
    private void ToggleSelectedAutoStart()
    {
        if (SelectedContainer is not { } selected) return;
        startupPreferences.SetContainerAutoStart(selected.Id, !SelectedAutoStartEnabled);
        OnPropertyChanged(nameof(SelectedAutoStartEnabled));
    }

    public override Task OnNavigatedToAsync() => RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var selectedId = SelectedContainer?.Id;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var rows = await containers.ListAsync(includeStopped: true);
            if (!_startupContainersProcessed)
            {
                _startupContainersProcessed = true;
                var availableIds = rows.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
                foreach (var staleId in startupPreferences.Current.AutoStartContainerIds.Where(id => !availableIds.Contains(id)).ToArray())
                {
                    startupPreferences.SetContainerAutoStart(staleId, enabled: false);
                }
                foreach (var row in rows.Where(item => startupPreferences.Current.AutoStartContainerIds.Contains(item.Id) && !string.Equals(item.State, "running", StringComparison.OrdinalIgnoreCase)))
                {
                    try { await containers.StartAsync(row.Id); }
                    catch (Exception ex) { ErrorMessage = $"Could not auto-start '{row.Name}': {ex.Message}"; }
                }
                if (startupPreferences.Current.AutoStartContainerIds.Count != 0) rows = await containers.ListAsync(includeStopped: true);
            }
            var visibleRows = IncludeStopped ? rows : rows.Where(item => string.Equals(item.State, "running", StringComparison.OrdinalIgnoreCase)).ToList();
            Items.Clear();
            foreach (var row in visibleRows) Items.Add(row);
            SelectedContainer = Items.FirstOrDefault(item => item.Id == selectedId);
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task OpenDetailsAsync()
    {
        if (SelectedContainer is null)
        {
            return;
        }

        var viewModel = services.GetRequiredService<ContainerDetailViewModel>();
        viewModel.SetContainer(SelectedContainer);
        await navigation.NavigateAsync(viewModel);
    }

    [RelayCommand]
    private async Task OpenRunWizardAsync()
    {
        var viewModel = services.GetRequiredService<RunContainerViewModel>();
        await navigation.NavigateAsync(viewModel);
    }

    [RelayCommand]
    private Task StartSelectedAsync() => RunSelectedActionAsync(
        () => containers.StartAsync(SelectedContainer!.Id));

    [RelayCommand]
    private Task StopSelectedAsync() => RunSelectedActionAsync(
        () => containers.StopAsync(SelectedContainer!.Id));

    [RelayCommand]
    private async Task RemoveSelectedAsync()
    {
        if (SelectedContainer is not { } selected ||
            !await confirmation.ConfirmAsync("Remove container", $"Permanently remove '{selected.Name}'?"))
        {
            return;
        }

        await RunSelectedActionAsync(() => containers.RemoveAsync(selected.Id));
    }

    private async Task RunSelectedActionAsync(Func<Task> action)
    {
        if (SelectedContainer is null)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await action();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
