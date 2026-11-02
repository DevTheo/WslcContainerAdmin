using Microsoft.Extensions.DependencyInjection;
using WslcContainerAdmin.ViewModels;

namespace WslcContainerAdmin.Services;

public static class WslcContainerAdminServiceCollectionExtensions
{
    public static IServiceCollection AddWslcContainerAdminServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IWslcCli, WslcCli>();
        services.AddSingleton<IWslcInventoryService, WslcInventoryService>();
        services.AddSingleton<IWslcContainerService, WslcContainerService>();
        services.AddSingleton<IWslcImageService, WslcImageService>();
        services.AddSingleton<IConfirmationService, AvaloniaConfirmationService>();
        services.AddSingleton<IContainerTerminalService, WindowsTerminalService>();
        services.AddSingleton<IResourceDetailsService, WslcResourceDetailsService>();
        services.AddTransient<VolumeDetailViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<ContainersViewModel>();
        services.AddTransient<ContainerDetailViewModel>();
        services.AddTransient<RunContainerViewModel>();
        services.AddTransient<ImagesViewModel>();
        services.AddTransient<ImageDetailViewModel>();
        services.AddTransient<NetworkDetailViewModel>();
        services.AddTransient<VolumesViewModel>();
        services.AddTransient<NetworksViewModel>();
        return services;
    }
}
