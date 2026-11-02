using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WslcContainerAdmin.Models;
using WslcContainerAdmin.Services;

namespace WslcContainerAdmin.ViewModels;

public partial class NetworkDetailViewModel(
    IResourceDetailsService details,
    INavigationService navigation,
    IServiceProvider services) : ViewModelBase
{
    private ViewModelBase? _returnTo;

    [ObservableProperty]
    private NetworkInfo? _network;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoContainers))]
    private NetworkDetails? _details;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public bool HasNoContainers => Details is not null && Details.Containers.Count == 0;

    public void SetNetwork(NetworkInfo network)
    {
        Network = network;
        _returnTo = navigation.Current;
    }

    public override async Task OnNavigatedToAsync() => await RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (Network is null)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Details = await details.GetNetworkDetailsAsync(Network.Name);
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
    private async Task BackAsync()
    {
        if (_returnTo is not null)
        {
            await navigation.NavigateAsync(_returnTo);
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync(NetworkEndpoint endpoint)
    {
        if (Network is null)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await details.DisconnectNetworkAsync(Network.Name, endpoint.ContainerId);
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

    [RelayCommand]
    private async Task OpenContainerDetailsAsync(NetworkEndpoint endpoint)
    {
        if (endpoint.Container is null)
        {
            return;
        }

        var viewModel = services.GetRequiredService<ContainerDetailViewModel>();
        viewModel.SetContainer(endpoint.Container);
        await navigation.NavigateAsync(viewModel);
    }
}
