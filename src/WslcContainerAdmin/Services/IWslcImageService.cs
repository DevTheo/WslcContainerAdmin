using WslcContainerAdmin.Models;

namespace WslcContainerAdmin.Services;

public interface IWslcImageService
{
    Task<IReadOnlyList<ImageInfo>> ListAsync(CancellationToken cancellationToken = default);
    Task<string> InspectAsync(string id, CancellationToken cancellationToken = default);
    Task PullAsync(string image, CancellationToken cancellationToken = default);
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);
}
