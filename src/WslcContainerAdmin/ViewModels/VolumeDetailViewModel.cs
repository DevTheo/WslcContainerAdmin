using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WslcContainerAdmin.Models;
using WslcContainerAdmin.Services;

namespace WslcContainerAdmin.ViewModels;

public partial class VolumeDetailViewModel(
    IResourceDetailsService details,
    IWslcInventoryService inventory,
    INavigationService navigation,
    IServiceProvider services,
    IConfirmationService confirmation) : ViewModelBase
{
    private ViewModelBase? _returnTo;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    private VolumeDetails? _volume;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public void SetVolume(VolumeInfo volume)
    {
        Volume = null;
        _selectedVolume = volume;
        _returnTo = navigation.Current;
    }

    private VolumeInfo? _selectedVolume;

    public override async Task OnNavigatedToAsync()
    {
        if (_selectedVolume is null)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Volume = await details.GetVolumeDetailsAsync(_selectedVolume.Name);
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

    public bool CanDelete => Volume is { IsUnused: true };

    [RelayCommand]
    private async Task BackAsync()
    {
        if (_returnTo is not null)
        {
            await navigation.NavigateAsync(_returnTo);
        }
    }

    [RelayCommand]
    private async Task RemoveAsync()
    {
        if (!CanDelete || Volume is not { } volume ||
            !await confirmation.ConfirmAsync("Remove volume", $"Permanently remove unused volume '{volume.Name}'?"))
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await inventory.RemoveVolumeAsync(volume.Name);
            if (_returnTo is not null)
            {
                await navigation.NavigateAsync(_returnTo);
            }
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
    private async Task OpenContainerDetailsAsync(ContainerInfo container)
    {
        var viewModel = services.GetRequiredService<ContainerDetailViewModel>();
        viewModel.SetContainer(container);
        await navigation.NavigateAsync(viewModel);
    }
}
