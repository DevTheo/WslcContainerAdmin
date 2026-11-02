using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using WslcContainerAdmin.Models;
using WslcContainerAdmin.Services;

namespace WslcContainerAdmin.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly IServiceProvider _services;
    private bool _isSynchronizingSelection;

    public MainViewModel(INavigationService navigation, IServiceProvider services)
    {
        _navigation = navigation;
        _services = services;

        NavigationItems = new ObservableCollection<NavigationItem>
        {
            new("Containers", typeof(ContainersViewModel)),
            new("Images", typeof(ImagesViewModel)),
            new("Volumes", typeof(VolumesViewModel)),
            new("Networks", typeof(NetworksViewModel)),
        };

        _navigation.Navigated += OnNavigated;
        SelectedNavigationItem = NavigationItems[0];
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    [ObservableProperty]
    private NavigationItem? _selectedNavigationItem;

    partial void OnSelectedNavigationItemChanged(NavigationItem? value)
    {
        if (value is null || _isSynchronizingSelection)
        {
            return;
        }

        var viewModel = (ViewModelBase)_services.GetRequiredService(value.ViewModelType);
        _ = _navigation.NavigateAsync(viewModel);
    }

    private void OnNavigated(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
        var item = NavigationItems.FirstOrDefault(candidate => candidate.ViewModelType == viewModel.GetType());
        if (item is not null && SelectedNavigationItem != item)
        {
            _isSynchronizingSelection = true;
            SelectedNavigationItem = item;
            _isSynchronizingSelection = false;
        }
    }
}
