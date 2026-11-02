using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WslcContainerAdmin.Models;
using WslcContainerAdmin.Services;

namespace WslcContainerAdmin.ViewModels;

public partial class ImagesViewModel(
    IWslcImageService images,
    INavigationService navigation,
    IServiceProvider services,
    IConfirmationService confirmation) : ViewModelBase
{
    public ObservableCollection<ImageInfo> Items { get; } = [];

    [ObservableProperty]
    private ImageInfo? _selectedImage;

    [ObservableProperty]
    private string _imageName = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _statusMessage;

    public override Task OnNavigatedToAsync() => RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var selectedId = SelectedImage?.Id;
            var rows = await images.ListAsync();
            Items.Clear();
            foreach (var row in rows)
            {
                Items.Add(row);
            }
            SelectedImage = Items.FirstOrDefault(item => item.Id == selectedId);
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
    private async Task PullAsync()
    {
        if (string.IsNullOrWhiteSpace(ImageName))
        {
            ErrorMessage = "Enter an image name, such as docker.io/library/alpine:latest.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = null;
        try
        {
            await images.PullAsync(ImageName);
            StatusMessage = $"Pulled {ImageName.Trim()}.";
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
    private async Task RemoveSelectedAsync()
    {
        if (SelectedImage is not { } selected ||
            !await confirmation.ConfirmAsync("Remove image", $"Permanently remove image '{selected.Repository}:{selected.Tag}'?"))
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await images.RemoveAsync(selected.Id);
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
    private async Task RunSelectedAsync()
    {
        if (SelectedImage is null)
        {
            return;
        }

        var wizard = services.GetRequiredService<RunContainerViewModel>();
        wizard.SetImage($"{SelectedImage.Repository}:{SelectedImage.Tag}");
        await navigation.NavigateAsync(wizard);
    }
    [RelayCommand]
    private async Task OpenDetailsAsync()
    {
        if (SelectedImage is null)
        {
            return;
        }

        var viewModel = services.GetRequiredService<ImageDetailViewModel>();
        viewModel.SetImage(SelectedImage);
        await navigation.NavigateAsync(viewModel);
    }
}
