using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WslcContainerAdmin.Models;
using WslcContainerAdmin.Services;

namespace WslcContainerAdmin.ViewModels;

public partial class NetworksViewModel(IWslcInventoryService inventory, INavigationService navigation, IServiceProvider services) : ViewModelBase
{
    public ObservableCollection<NetworkInfo> Networks { get; } = [];

    [ObservableProperty]
    private NetworkInfo? _selectedNetwork;

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
            var rows = await inventory.ListNetworksAsync();
            Networks.Clear();
            foreach (var row in rows)
            {
                Networks.Add(row);
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
    private async Task OpenDetailsAsync()
    {
        if (SelectedNetwork is null)
        {
            return;
        }

        var viewModel = services.GetRequiredService<NetworkDetailViewModel>();
        viewModel.SetNetwork(SelectedNetwork);
        await navigation.NavigateAsync(viewModel);
    }
}
