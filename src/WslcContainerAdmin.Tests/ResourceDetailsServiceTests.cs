using WslcContainerAdmin.Models;
using WslcContainerAdmin.Services;
using Xunit;

namespace WslcContainerAdmin.Tests;

public sealed class ResourceDetailsServiceTests
{
    [Fact]
    public async Task Volume_details_include_creation_metadata_and_using_containers()
    {
        var cli = new FakeCli();
        var service = new WslcResourceDetailsService(cli, new WslcContainerService(cli));

        var details = await service.GetVolumeDetailsAsync("data");

        Assert.Equal("created", details.CreatedAt);
        Assert.Equal("/var/lib/volumes/data", details.Mountpoint);
        Assert.Equal("guest", details.Driver);
        Assert.Equal("web", Assert.Single(details.UsingContainers).Name);
        Assert.False(details.IsUnused);
    }

    [Fact]
    public async Task Network_details_include_ipam_options_and_attached_container_addresses()
    {
        var cli = new FakeCli();
        var service = new WslcResourceDetailsService(cli, new WslcContainerService(cli));

        var details = await service.GetNetworkDetailsAsync("backend");

        Assert.Equal("10.1.0.0/16", Assert.Single(details.IpamConfigs).Subnet);
        Assert.Equal("10.1.0.1", details.IpamConfigs[0].Gateway);
        Assert.Equal("mtu", Assert.Single(details.Options).Key);
        var endpoint = Assert.Single(details.Containers);
        Assert.Equal("web", endpoint.Name);
        Assert.Equal("10.1.0.2/16", endpoint.IPv4Address);
        Assert.Equal("web", endpoint.Container?.Name);

        await service.DisconnectNetworkAsync("backend", endpoint.ContainerId);
        Assert.Equal(new[] { "network", "disconnect", "backend", endpoint.ContainerId }, cli.LastArguments);
    }

    private sealed class FakeCli : IWslcCli
    {
        public IReadOnlyList<string>? LastArguments { get; private set; }

        public Task<WslcCliResult> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
        {
            LastArguments = arguments.ToArray();
            var output = arguments[0] switch
            {
                "volume" => "[{\"Name\":\"data\",\"CreatedAt\":\"created\",\"Mountpoint\":\"/var/lib/volumes/data\",\"Driver\":\"guest\",\"Scope\":\"local\"}]",
                "network" when arguments[1] == "inspect" => "[{\"Id\":\"network-id\",\"Name\":\"backend\",\"Driver\":\"bridge\",\"Scope\":\"local\",\"Attachable\":true,\"Internal\":false,\"EnableIPv4\":true,\"EnableIPv6\":false,\"Created\":\"today\",\"IPAM\":{\"Driver\":\"default\",\"Config\":[{\"Subnet\":\"10.1.0.0/16\",\"Gateway\":\"10.1.0.1\"}]},\"Options\":{\"mtu\":\"1500\"},\"Containers\":{\"container-id-full\":{\"Name\":\"/web\",\"EndpointID\":\"endpoint\",\"MacAddress\":\"02:42:0a:01:00:02\",\"IPv4Address\":\"10.1.0.2/16\",\"IPv6Address\":\"\"}}}]",
                "network" => string.Empty,
                "container" when arguments[1] == "list" => "{\"ID\":\"container-id\",\"Names\":\"web\",\"Image\":\"nginx\",\"Command\":\"nginx\",\"CreatedAt\":\"today\",\"Status\":\"Up\",\"State\":\"running\",\"Ports\":\"\",\"Networks\":\"backend\",\"Platform\":\"linux/amd64\",\"Labels\":\"{}\"}",
                "container" when arguments[1] == "inspect" => "{\"Mounts\":[{\"Name\":\"data\"}]}",
                _ => string.Empty,
            };
            return Task.FromResult(new WslcCliResult(0, output, string.Empty));
        }

        public Task<WslcCliResult> RunStreamingAsync(IReadOnlyList<string> arguments, Func<string, Task> onStandardOutputLine, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WslcCliResult(0, string.Empty, string.Empty));
    }
}
