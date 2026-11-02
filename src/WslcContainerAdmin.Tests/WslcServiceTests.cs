using WslcContainerAdmin.Services;
using WslcContainerAdmin.Models;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using WslcContainerAdmin.ViewModels;

namespace WslcContainerAdmin.Tests;

public sealed class WslcServiceTests
{
    [Fact]
    public async Task Container_list_parses_real_cli_field_names_and_requests_all_rows_as_json()
    {
        var cli = new FakeCli("{\"ID\":\"abc\",\"Names\":\"web\",\"Image\":\"nginx:latest\",\"Command\":\"nginx\",\"CreatedAt\":\"today\",\"Status\":\"Up\",\"State\":\"running\",\"Ports\":\"80/tcp\",\"Networks\":\"bridge\",\"Platform\":{\"os\":\"linux\"},\"Labels\":\"{}\"}");
        var service = new WslcContainerService(cli);

        var row = Assert.Single(await service.ListAsync(includeStopped: true));

        Assert.Equal("abc", row.Id);
        Assert.Equal("web", row.Name);
        Assert.Equal("{\"os\":\"linux\"}", row.Platform);
        Assert.Equal(new[] { "container", "list", "--all", "--format", "json" }, cli.LastArguments);
    }

    [Fact]
    public async Task Run_forwards_all_requested_flags_as_distinct_arguments()
    {
        var cli = new FakeCli();
        var service = new WslcContainerService(cli);

        await service.RunAsync(new ContainerRunOptions(
            "alpine:latest", "worker", "sh", ["-c", "echo hello world"], [new PortMapping(8080, 80)], [new VolumeMount("data", "/data")], ["A=B"],
            false, true, true, true, 30, "/entry", "/app", "1000", true, "backend", "api", "example.local", "172.20.0.5",
            ["1.1.1.1", "8.8.8.8"], ".env", ["app=demo"], "512M", "1.5", "64M"));

        Assert.Equal(new[]
        {
            "container", "run", "--name", "worker", "--rm", "--interactive", "--gpus", "all", "--stop-timeout", "30",
            "--entrypoint", "/entry", "--workdir", "/app", "--user", "1000", "--tty", "--network", "backend", "--hostname", "api", "--domainname", "example.local", "--ip", "172.20.0.5",
            "--dns", "1.1.1.1", "--dns", "8.8.8.8", "--env-file", ".env", "--label", "app=demo", "--memory", "512M", "--cpus", "1.5", "--shm-size", "64M",
            "--publish", "8080:80", "--volume", "data:/data", "--env", "A=B", "alpine:latest", "sh", "-c", "echo hello world",
        }, cli.LastArguments);
    }

    [Fact]
    public async Task Run_defaults_to_background_without_other_optional_flags()
    {
        var cli = new FakeCli();
        var service = new WslcContainerService(cli);

        await service.RunAsync(new ContainerRunOptions("alpine:latest", null, null, [], [], [], []));

        Assert.Equal(["container", "run", "--detach", "alpine:latest"], cli.LastArguments);
    }

    [Fact]
    public async Task Image_inspect_forwards_id_as_argument()
    {
        var cli = new FakeCli("{\"Config\":{\"ExposedPorts\":{\"80/tcp\":{}}}}");

        await new WslcImageService(cli).InspectAsync("abc123");

        Assert.Equal(new[] { "image", "inspect", "abc123" }, cli.LastArguments);
    }

    [Fact]
    public async Task Image_and_inventory_services_parse_json_lines()
    {
        var imageCli = new FakeCli("{\"ID\":\"i1\",\"Repository\":\"alpine\",\"Tag\":\"latest\",\"Size\":\"5MB\",\"CreatedSince\":\"yesterday\",\"Digest\":\"sha256:x\",\"Containers\":\"0\"}");
        var image = Assert.Single(await new WslcImageService(imageCli).ListAsync());
        Assert.Equal("alpine", image.Repository);

        var inventoryCli = new FakeCli(
            "{\"Name\":\"data\",\"Driver\":\"local\",\"Scope\":\"local\",\"Mountpoint\":\"/data\"}",
            string.Empty,
            "{\"ID\":\"n1\",\"Name\":\"bridge\",\"Driver\":\"bridge\",\"Scope\":\"local\",\"IPv4\":\"true\",\"IPv6\":\"false\",\"Internal\":\"false\",\"CreatedAt\":\"today\"}");
        var inventory = new WslcInventoryService(inventoryCli);
        Assert.Equal("data", Assert.Single(await inventory.ListVolumesAsync()).Name);
        var network = Assert.Single(await inventory.ListNetworksAsync());
        Assert.True(network.IPv4);
        Assert.False(network.IPv6);
    }

    [Fact]
    public void Composition_registers_cli_services_and_all_viewmodels()
    {
        var services = new ServiceCollection().AddWslcContainerAdminServices();
        var expected = new[]
        {
            typeof(IWslcCli), typeof(IWslcInventoryService), typeof(IWslcContainerService),
            typeof(IWslcImageService), typeof(IConfirmationService), typeof(INavigationService), typeof(IContainerTerminalService), typeof(IResourceDetailsService),
            typeof(MainViewModel), typeof(ContainersViewModel), typeof(ContainerDetailViewModel), typeof(VolumeDetailViewModel), typeof(NetworkDetailViewModel),
            typeof(RunContainerViewModel), typeof(ImagesViewModel), typeof(ImageDetailViewModel), typeof(VolumesViewModel), typeof(NetworksViewModel),
        };
        foreach (var serviceType in expected)
        {
            Assert.Contains(services, descriptor => descriptor.ServiceType == serviceType);
        }
    }

    private sealed class FakeCli(params string[] outputs) : IWslcCli
    {
        private int _index;
        public IReadOnlyList<string>? LastArguments { get; private set; }

        public Task<WslcCliResult> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
        {
            LastArguments = arguments.ToArray();
            var output = outputs.Length == 0 ? string.Empty : outputs[Math.Min(_index++, outputs.Length - 1)];
            return Task.FromResult(new WslcCliResult(0, output, string.Empty));
        }

        public Task<WslcCliResult> RunStreamingAsync(IReadOnlyList<string> arguments, Func<string, Task> onStandardOutputLine, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WslcCliResult(0, string.Empty, string.Empty));
    }
}
