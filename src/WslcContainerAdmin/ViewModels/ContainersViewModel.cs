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
    IConfirmationService confirmation) : ViewModelBase
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

    public override Task OnNavigatedToAsync() => RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var selectedId = SelectedContainer?.Id;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var rows = await containers.ListAsync(IncludeStopped);
            Items.Clear();
            foreach (var row in rows)
            {
                Items.Add(row);
            }
            SelectedContainer = Items.FirstOrDefault(item => item.Id == selectedId);
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
