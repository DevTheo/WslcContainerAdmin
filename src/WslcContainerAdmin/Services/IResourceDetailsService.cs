using WslcContainerAdmin.Models;

namespace WslcContainerAdmin.Services;

public interface IResourceDetailsService
{
    Task<VolumeDetails> GetVolumeDetailsAsync(string name, CancellationToken cancellationToken = default);
    Task<NetworkDetails> GetNetworkDetailsAsync(string name, CancellationToken cancellationToken = default);
    Task DisconnectNetworkAsync(string networkName, string containerId, CancellationToken cancellationToken = default);
}
