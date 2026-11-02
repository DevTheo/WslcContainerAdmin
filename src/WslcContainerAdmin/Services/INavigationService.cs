using WslcContainerAdmin.ViewModels;

namespace WslcContainerAdmin.Services;

public interface INavigationService
{
    ViewModelBase? Current { get; }

    event Action<ViewModelBase>? Navigated;

    Task NavigateAsync(ViewModelBase viewModel);
}
