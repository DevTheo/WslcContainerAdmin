using Microsoft.Extensions.DependencyInjection;
using WslcContainerAdmin.Models;
using WslcContainerAdmin.Services;
using WslcContainerAdmin.ViewModels;
using Xunit;

namespace WslcContainerAdmin.Tests;

public sealed class ResourceDetailsViewModelTests
{
    private static readonly ContainerInfo Container = new("abc123", "web", "nginx", "nginx", "today", "Up", "running", "", "bridge", "linux/amd64", "{}");

    [Fact]
    public async Task Volume_delete_is_enabled_only_when_details_confirm_no_using_containers()
    {
        var service = new FakeDetailsService
        {
            Volume = new VolumeDetails("data", "today", "/data", "guest", "local", [Container]),
        };
        var vm = new VolumeDetailViewModel(
            service,
            new FakeInventory(),
            new NavigationService(),
            new ServiceCollection().BuildServiceProvider(),
            new FakeConfirmation());
        vm.SetVolume(new VolumeInfo("data", "guest", "local", "/data", null));

        await vm.OnNavigatedToAsync();
        Assert.False(vm.CanDelete);

        service.Volume = new VolumeDetails("data", "today", "/data", "guest", "local", []);
        await vm.OnNavigatedToAsync();
        Assert.True(vm.CanDelete);
    }

    private sealed class FakeDetailsService : IResourceDetailsService
    {
        public VolumeDetails Volume { get; set; } = new("data", "today", "/data", "guest", "local", []);
        public NetworkDetails Network { get; set; } = new("id", "bridge", "bridge", "local", false, false, true, false, "today", "default", [], [], []);
        public Task<VolumeDetails> GetVolumeDetailsAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult(Volume);
        public Task<NetworkDetails> GetNetworkDetailsAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult(Network);
        public Task DisconnectNetworkAsync(string networkName, string containerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeInventory : IWslcInventoryService
    {
        public Task<IReadOnlyList<VolumeInfo>> ListVolumesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<VolumeInfo>>([]);
        public Task CreateVolumeAsync(string name, string driver = "guest", CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveVolumeAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<NetworkInfo>> ListNetworksAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<NetworkInfo>>([]);
    }

    private sealed class FakeConfirmation : IConfirmationService
    {
        public Task<bool> ConfirmAsync(string title, string message) => Task.FromResult(true);
    }
}
