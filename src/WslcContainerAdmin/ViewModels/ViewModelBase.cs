using CommunityToolkit.Mvvm.ComponentModel;

namespace WslcContainerAdmin.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    /// <summary>Called by the navigation service after this view model becomes the current page.</summary>
    public virtual Task OnNavigatedToAsync() => Task.CompletedTask;

    /// <summary>Called by the navigation service before another page replaces this one.</summary>
    public virtual void OnNavigatedFrom()
    {
    }
}
