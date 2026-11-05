using Microsoft.Extensions.DependencyInjection;
using WslcContainerAdmin.Models;
using WslcContainerAdmin.Services;
using WslcContainerAdmin.ViewModels;
using Xunit;

namespace WslcContainerAdmin.Tests;

public sealed class ViewModelTests
{
    private static readonly ContainerInfo Container = new("id1", "test", "alpine:latest", "sh", "now", "Up", "running", "80:80", "bridge", "linux/amd64", "{}");
    private static readonly ImageInfo Image = new("img1", "alpine", "latest", "5MB", "1 day ago", "sha256:x", "0");

    [Fact]
    public async Task Containers_refresh_and_actions_use_selected_item()
    {
        var fake = new FakeContainers();
        var vm = new ContainersViewModel(fake, new NavigationService(), new ServiceCollection().BuildServiceProvider(), new FakeConfirmation(true), new FakeStartupPreferences());
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.Equal(Container, Assert.Single(vm.Items));
        vm.SelectedContainer = Container;
        await vm.StartSelectedCommand.ExecuteAsync(null);
        Assert.Equal("id1", Assert.Single(fake.Started));
        await vm.StopSelectedCommand.ExecuteAsync(null);
        await vm.RemoveSelectedCommand.ExecuteAsync(null);
        Assert.Equal("id1", Assert.Single(fake.Removed));
    }

    [Fact]
    public async Task Containers_start_only_configured_stopped_containers_on_first_refresh()
    {
        var fake = new FakeContainers();
        fake.Rows.Clear();
        fake.Rows.Add(Container with { State = "exited" });
        fake.Rows.Add(Container with { Id = "running-id", Name = "already-running", State = "running" });
        var preferences = new FakeStartupPreferences();
        preferences.SetContainerAutoStart("id1", true);
        preferences.SetContainerAutoStart("running-id", true);
        var vm = new ContainersViewModel(fake, new NavigationService(), new ServiceCollection().BuildServiceProvider(), new FakeConfirmation(false), preferences);

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "id1" }, fake.Started);
    }

    [Fact]
    public async Task Container_detail_refreshes_inspect_and_stats()
    {
        var fake = new FakeContainers();
        var vm = new ContainerDetailViewModel(fake, new NavigationService(), new ServiceCollection().BuildServiceProvider(), new FakeConfirmation(false));
        vm.SetContainer(Container);
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.Equal("{ inspect }", vm.InspectJson);
        Assert.Equal("{ stats }", vm.StatsJson);
    }

    [Fact]
    public void Container_detail_opens_terminal_only_for_running_containers()
    {
        var terminal = new FakeContainerTerminal();
        var services = new ServiceCollection().AddSingleton<IContainerTerminalService>(terminal).BuildServiceProvider();
        var vm = new ContainerDetailViewModel(new FakeContainers(), new NavigationService(), services, new FakeConfirmation(false));
        vm.SetContainer(Container);

        Assert.True(vm.OpenTerminalCommand.CanExecute(null));
        vm.OpenTerminalCommand.Execute(null);
        Assert.Equal(Container.Id, terminal.ContainerId);

        vm.SetContainer(Container with { State = "exited" });
        Assert.False(vm.OpenTerminalCommand.CanExecute(null));
    }

    [Fact]
    public async Task Container_detail_back_returns_to_originating_container_list()
    {
        var navigation = new NavigationService();
        var containers = new FakeContainers();
        var services = new ServiceCollection().BuildServiceProvider();
        var list = new ContainersViewModel(containers, navigation, services, new FakeConfirmation(false), new FakeStartupPreferences());
        await navigation.NavigateAsync(list);

        var detail = new ContainerDetailViewModel(containers, navigation, services, new FakeConfirmation(false));
        detail.SetContainer(Container);
        await navigation.NavigateAsync(detail);
        await detail.BackCommand.ExecuteAsync(null);

        Assert.Same(list, navigation.Current);
    }

    [Fact]
    public async Task Run_wizard_validates_image_and_passes_structured_mappings()
    {
        var images = new FakeImages();
        var containers = new FakeContainers();
        var services = new ServiceCollection().BuildServiceProvider();
        var vm = new RunContainerViewModel(images, new FakeInventory(), containers, new NavigationService(), services);
        Assert.True(vm.RunInBackground);
        await vm.NextCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage);
        vm.ImageName = "alpine:latest";
        await vm.NextCommand.ExecuteAsync(null);
        vm.ContainerName = "worker";
        vm.Arguments = "-c\necho ok";
        vm.VolumeMounts.Add(new VolumeMountItem { SelectedVolume = new VolumeInfo("data", "local", "local", "/data"), ContainerPath = "/data" });
        vm.PortMappings.Add(new PortMappingItem { HostPort = 8080, ContainerPort = 80 });
        vm.RunInBackground = false;
        vm.RemoveOnExit = true;
        vm.KeepStdinOpen = true;
        vm.PassAllGpus = true;
        vm.StopTimeoutSeconds = "45";
        vm.Entrypoint = "/entry";
        vm.WorkingDirectory = "/app";
        vm.UserName = "1000";
        vm.ConsoleMode = "Interactive + TTY (-i -t)";
        vm.SelectedNetwork = new NetworkInfo("net1", "backend", "bridge", "local", true, false, false, "today");
        vm.Hostname = "api";
        vm.DomainName = "example.local";
        vm.Ipv4Address = "172.20.0.5";
        vm.DnsServers = "1.1.1.1\n8.8.8.8";
        vm.EnvironmentFile = ".env";
        vm.Labels = "app=demo";
        vm.MemoryLimit = "512M";
        vm.CpuLimit = "1.5";
        vm.SharedMemorySize = "64M";
        vm.EnvironmentVariables = "MODE=test";
        await vm.NextCommand.ExecuteAsync(null);
        await vm.LaunchCommand.ExecuteAsync(null);
        var options = Assert.IsType<ContainerRunOptions>(containers.LastRun);
        Assert.Equal(["-c", "echo ok"], options.Arguments);
        Assert.Equal(new[] { new PortMapping(8080, 80) }, options.PublishedPorts);
        Assert.Equal(new[] { new VolumeMount("data", "/data") }, options.VolumeMounts);
        Assert.Equal(["MODE=test"], options.EnvironmentVariables);
        Assert.False(options.RunInBackground);
        Assert.True(options.RemoveOnExit);
        Assert.True(options.KeepStdinOpen);
        Assert.True(options.PassAllGpus);
        Assert.Equal(45, options.StopTimeoutSeconds);
        Assert.Equal("/entry", options.Entrypoint);
        Assert.Equal("/app", options.WorkingDirectory);
        Assert.Equal("1000", options.User);
        Assert.True(options.KeepStdinOpen);
        Assert.True(options.AllocateTty);
        Assert.Equal("backend", options.Network);
        Assert.Equal("api", options.Hostname);
        Assert.Equal("example.local", options.DomainName);
        Assert.Equal("172.20.0.5", options.Ipv4Address);
        Assert.Equal(["1.1.1.1", "8.8.8.8"], options.DnsServers);
        Assert.Equal(".env", options.EnvironmentFile);
        Assert.Equal(["app=demo"], options.Labels);
        Assert.Equal("512M", options.MemoryLimit);
        Assert.Equal("1.5", options.CpuLimit);
        Assert.Equal("64M", options.SharedMemorySize);
    }

    [Fact]
    public async Task Run_wizard_pulls_image_and_loads_available_networks()
    {
        var images = new FakeImages();
        var inventory = new FakeInventory();
        var vm = new RunContainerViewModel(images, inventory, new FakeContainers(), new NavigationService(), new ServiceCollection().BuildServiceProvider());

        await vm.OnNavigatedToAsync();
        vm.ImageName = "alpine:latest";
        await vm.PullImageCommand.ExecuteAsync(null);

        Assert.Equal("alpine:latest", images.Pulled);
        Assert.Equal("Pulled alpine:latest.", vm.StatusMessage);
        Assert.Equal("bridge", Assert.Single(vm.AvailableNetworks).Name);
    }

    [Fact]
    public async Task Run_wizard_loads_volume_inventory_and_exposed_ports()
    {
        var images = new FakeImages { InspectionJson = "[{\"Config\":{\"ExposedPorts\":{\"80/tcp\":{},\"443/udp\":{},\"invalid/tcp\":{}}}}]" };
        var vm = new RunContainerViewModel(images, new FakeInventory(), new FakeContainers(), new NavigationService(), new ServiceCollection().BuildServiceProvider());

        await vm.OnNavigatedToAsync();
        vm.SelectedImage = Image;
        await vm.UseSelectedImageCommand.ExecuteAsync(null);

        Assert.Equal("data", Assert.Single(vm.AvailableVolumes).Name);
        Assert.Equal(new[] { 80, 443 }, vm.ImageExposedPorts);
        Assert.True(vm.HasImageExposedPorts);
        vm.AddExposedPortMappingCommand.Execute(80);
        var mapping = Assert.Single(vm.PortMappings);
        Assert.Equal(0, mapping.HostPort);
        Assert.Equal(80, mapping.ContainerPort);
    }

    [Fact]
    public async Task Run_wizard_rejects_incomplete_mappings_before_launch()
    {
        var containers = new FakeContainers();
        var vm = new RunContainerViewModel(new FakeImages(), new FakeInventory(), containers, new NavigationService(), new ServiceCollection().BuildServiceProvider())
        {
            ImageName = "alpine:latest",
        };
        vm.VolumeMounts.Add(new VolumeMountItem());
        vm.PortMappings.Add(new PortMappingItem { HostPort = 8080, ContainerPort = 0 });

        await vm.LaunchCommand.ExecuteAsync(null);
        Assert.Null(containers.LastRun);
        Assert.Contains("volume", vm.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        vm.VolumeMounts[0].SelectedVolume = new VolumeInfo("data", "local", "local", "/data");
        vm.VolumeMounts[0].ContainerPath = "/data";
        await vm.LaunchCommand.ExecuteAsync(null);
        Assert.Null(containers.LastRun);
        Assert.Contains("port", vm.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Run_wizard_validates_stop_timeout_and_accepts_no_timeout()
    {
        var containers = new FakeContainers();
        var vm = new RunContainerViewModel(new FakeImages(), new FakeInventory(), containers, new NavigationService(), new ServiceCollection().BuildServiceProvider())
        {
            ImageName = "alpine:latest",
            StopTimeoutSeconds = "-2",
        };

        await vm.LaunchCommand.ExecuteAsync(null);
        Assert.Null(containers.LastRun);
        Assert.Contains("timeout", vm.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        vm.StopTimeoutSeconds = "-1";
        await vm.LaunchCommand.ExecuteAsync(null);
        Assert.Equal(-1, containers.LastRun?.StopTimeoutSeconds);
    }



    [Fact]
    public async Task Images_refresh_pull_remove_and_run_selected()
    {
        var images = new FakeImages();
        var containers = new FakeContainers();
        var navigation = new NavigationService();
        var services = new ServiceCollection().AddTransient<RunContainerViewModel>(_ => new(images, new FakeInventory(), containers, navigation, new ServiceCollection().BuildServiceProvider())).BuildServiceProvider();
        var vm = new ImagesViewModel(images, navigation, services, new FakeConfirmation(true));
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.Equal(Image, Assert.Single(vm.Items));
        vm.ImageName = "busybox:latest";
        await vm.PullCommand.ExecuteAsync(null);
        Assert.Equal("busybox:latest", images.Pulled);
        vm.SelectedImage = Image;
        await vm.RemoveSelectedCommand.ExecuteAsync(null);
        Assert.Equal("img1", images.Removed);
        await vm.RunSelectedCommand.ExecuteAsync(null);
        Assert.IsType<RunContainerViewModel>(navigation.Current);
    }

    [Fact]
    public async Task Volume_and_network_viewmodels_refresh_inventory()
    {
        var inventory = new FakeInventory();
        var volumes = new VolumesViewModel(inventory, new FakeConfirmation(true), new NavigationService(), new ServiceCollection().BuildServiceProvider());
        var networks = new NetworksViewModel(inventory, new NavigationService(), new ServiceCollection().BuildServiceProvider());
        await volumes.RefreshCommand.ExecuteAsync(null);
        await networks.RefreshCommand.ExecuteAsync(null);
        Assert.Equal("data", Assert.Single(volumes.Volumes).Name);
        Assert.Equal("bridge", Assert.Single(networks.Networks).Name);
    }

    [Fact]
    public async Task Main_navigation_starts_on_containers_and_tracks_navigation()
    {
        var navigation = new NavigationService();
        var containers = new FakeContainers();
        var inventory = new FakeInventory();
        var images = new FakeImages();
        var confirmation = new FakeConfirmation(false);
        var provider = new ServiceCollection()
            .AddSingleton<IWslcContainerService>(containers)
            .AddSingleton<IWslcInventoryService>(inventory)
            .AddSingleton<IWslcImageService>(images)
            .AddSingleton<IConfirmationService>(confirmation)
            .AddSingleton<IStartupPreferencesService>(new FakeStartupPreferences())
            .AddSingleton<INavigationService>(navigation)
            .AddTransient<ContainersViewModel>()
            .AddTransient<ImagesViewModel>()
            .AddTransient<VolumesViewModel>()
            .AddTransient<NetworksViewModel>()
            .BuildServiceProvider();
        var vm = new MainViewModel(navigation, provider);
        await Task.Delay(50);
        Assert.Equal(typeof(ContainersViewModel), navigation.Current?.GetType());
        Assert.Equal("Containers", vm.SelectedNavigationItem?.Title);
    }
    [Fact]
    public async Task Image_detail_shows_inspect_data_and_returns_to_images_list()
    {
        var images = new FakeImages
        {
            InspectionJson = "[{\"Id\":\"sha256:img\",\"Size\":1234,\"Created\":\"2026-10-30T00:00:00Z\",\"Os\":\"linux\",\"Architecture\":\"amd64\",\"RepoTags\":[\"alpine:latest\"],\"Config\":{\"Cmd\":[\"/bin/sh\"],\"Entrypoint\":[\"/entry\"],\"ExposedPorts\":{\"80/tcp\":{}},\"Env\":[\"A=B\"],\"Labels\":{\"team\":\"dev\"}},\"RootFS\":{\"Layers\":[\"sha256:layer\"]}}]",
        };
        var navigation = new NavigationService();
        var provider = new ServiceCollection()
            .AddTransient<ImageDetailViewModel>(_ => new(images, navigation))
            .BuildServiceProvider();
        var list = new ImagesViewModel(images, navigation, provider, new FakeConfirmation(false));
        await navigation.NavigateAsync(list);
        list.SelectedImage = Image;

        await list.OpenDetailsCommand.ExecuteAsync(null);
        var detail = Assert.IsType<ImageDetailViewModel>(navigation.Current);
        Assert.Contains(detail.Configuration, row => row.Name == "Command" && row.Value.Contains("/bin/sh", StringComparison.Ordinal));
        Assert.Equal("sha256:layer", Assert.Single(detail.LayerDigests));

        await detail.BackCommand.ExecuteAsync(null);
        Assert.Same(list, navigation.Current);
    }

    private sealed class FakeContainerTerminal : IContainerTerminalService
    {
        public string? ContainerId { get; private set; }
        public void OpenShell(string containerId) => ContainerId = containerId;
    }

    private sealed class FakeConfirmation(bool answer) : IConfirmationService
    {
        public Task<bool> ConfirmAsync(string title, string message) => Task.FromResult(answer);
    }

    private sealed class FakeImages : IWslcImageService
    {
        public string? Pulled { get; private set; }
        public string? Removed { get; private set; }
        public Task<IReadOnlyList<ImageInfo>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ImageInfo>>([Image]);
        public string InspectionJson { get; set; } = "{}";
        public Task<string> InspectAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(InspectionJson);
        public Task PullAsync(string image, CancellationToken cancellationToken = default) { Pulled = image; return Task.CompletedTask; }
        public Task RemoveAsync(string id, CancellationToken cancellationToken = default) { Removed = id; return Task.CompletedTask; }
    }

    private sealed class FakeStartupPreferences : IStartupPreferencesService
    {
        private readonly HashSet<string> _ids = new(StringComparer.Ordinal);
        public StartupPreferences Current { get; private set; } = new(false, new HashSet<string>(StringComparer.Ordinal));
        public void SetContainerAutoStart(string containerId, bool enabled)
        {
            if (enabled) _ids.Add(containerId); else _ids.Remove(containerId);
            Current = Current with { AutoStartContainerIds = new HashSet<string>(_ids, StringComparer.Ordinal) };
        }
        public void SetLaunchAtSignIn(bool enabled) => Current = Current with { LaunchAtSignIn = enabled };
    }

    private sealed class FakeContainers : IWslcContainerService
    {
        public List<string> Started { get; } = [];
        public List<string> Removed { get; } = [];
        public List<ContainerInfo> Rows { get; } = [Container];
        public ContainerRunOptions? LastRun { get; private set; }
        public Task<IReadOnlyList<ContainerInfo>> ListAsync(bool includeStopped, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContainerInfo>>(Rows.ToList());
        public Task RunAsync(ContainerRunOptions options, CancellationToken cancellationToken = default) { LastRun = options; return Task.CompletedTask; }
        public Task StartAsync(string id, CancellationToken cancellationToken = default) { Started.Add(id); return Task.CompletedTask; }
        public Task StopAsync(string id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveAsync(string id, CancellationToken cancellationToken = default) { Removed.Add(id); return Task.CompletedTask; }
        public Task<string> InspectAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult("{ inspect }");
        public Task<string> GetLogsAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult("log");
        public Task<string> GetStatsAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult("{ stats }");
        public Task FollowLogsAsync(string id, Func<string, Task> onLine, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeInventory : IWslcInventoryService
    {
        public Task<IReadOnlyList<VolumeInfo>> ListVolumesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<VolumeInfo>>([new("data", "local", "local", "/data", true)]);
        public Task CreateVolumeAsync(string name, string driver = "guest", CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveVolumeAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<NetworkInfo>> ListNetworksAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<NetworkInfo>>([new("net1", "bridge", "bridge", "local", true, false, false, "today")]);
    }
}
