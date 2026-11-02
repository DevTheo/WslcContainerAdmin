using WslcContainerAdmin.ViewModels;

namespace WslcContainerAdmin.Services;

public sealed class NavigationService : INavigationService
{
    public ViewModelBase? Current { get; private set; }

    public event Action<ViewModelBase>? Navigated;

    public async Task NavigateAsync(ViewModelBase viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        Current?.OnNavigatedFrom();
        Current = viewModel;
        Navigated?.Invoke(viewModel);
        await viewModel.OnNavigatedToAsync();
    }
}
