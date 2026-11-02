using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WslcContainerAdmin.Models;
using WslcContainerAdmin.Services;

namespace WslcContainerAdmin.ViewModels;

public partial class VolumesViewModel(IWslcInventoryService inventory, IConfirmationService confirmation, INavigationService navigation, IServiceProvider services) : ViewModelBase
{
    public ObservableCollection<VolumeInfo> Volumes { get; } = [];

    [ObservableProperty]
    private VolumeInfo? _selectedVolume;

    [ObservableProperty]
    private string _newVolumeName = string.Empty;

    [ObservableProperty]
    private string _newVolumeDriver = "guest";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public override Task OnNavigatedToAsync() => RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var selectedName = SelectedVolume?.Name;
            var rows = await inventory.ListVolumesAsync();
            Volumes.Clear();
            foreach (var row in rows)
            {
                Volumes.Add(row);
            }
            SelectedVolume = Volumes.FirstOrDefault(item => item.Name == selectedName);
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
    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewVolumeName))
        {
            ErrorMessage = "Enter a volume name.";
            return;
        }
        await RunMutationAsync(() => inventory.CreateVolumeAsync(NewVolumeName.Trim(), NewVolumeDriver.Trim()));
        if (ErrorMessage is null)
        {
            NewVolumeName = string.Empty;
        }
    }

    [RelayCommand]
    private async Task OpenDetailsAsync()
    {
        if (SelectedVolume is null)
        {
            return;
        }

        var viewModel = services.GetRequiredService<VolumeDetailViewModel>();
        viewModel.SetVolume(SelectedVolume);
        await navigation.NavigateAsync(viewModel);
    }

    [RelayCommand]
    private async Task RemoveSelectedAsync()
    {
        if (SelectedVolume is not { } selected ||
            !await confirmation.ConfirmAsync("Remove volume", $"Permanently remove volume '{selected.Name}'?"))
        {
            return;
        }
        await RunMutationAsync(() => inventory.RemoveVolumeAsync(selected.Name));
    }

    private async Task RunMutationAsync(Func<Task> operation)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await operation();
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
