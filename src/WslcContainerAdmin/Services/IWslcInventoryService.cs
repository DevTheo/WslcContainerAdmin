using WslcContainerAdmin.Models;

namespace WslcContainerAdmin.Services;

public interface IWslcInventoryService
{
    Task<IReadOnlyList<VolumeInfo>> ListVolumesAsync(CancellationToken cancellationToken = default);
    Task CreateVolumeAsync(string name, string driver = "guest", CancellationToken cancellationToken = default);
    Task RemoveVolumeAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkInfo>> ListNetworksAsync(CancellationToken cancellationToken = default);
}
