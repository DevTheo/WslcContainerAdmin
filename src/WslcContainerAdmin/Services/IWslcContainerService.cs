using WslcContainerAdmin.Models;

namespace WslcContainerAdmin.Services;

public interface IWslcContainerService
{
    Task<IReadOnlyList<ContainerInfo>> ListAsync(bool includeStopped, CancellationToken cancellationToken = default);
    Task RunAsync(ContainerRunOptions options, CancellationToken cancellationToken = default);
    Task StartAsync(string id, CancellationToken cancellationToken = default);
    Task StopAsync(string id, CancellationToken cancellationToken = default);
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);
    Task<string> InspectAsync(string id, CancellationToken cancellationToken = default);
    Task<string> GetLogsAsync(string id, CancellationToken cancellationToken = default);
    Task<string> GetStatsAsync(string id, CancellationToken cancellationToken = default);
    Task FollowLogsAsync(string id, Func<string, Task> onLine, CancellationToken cancellationToken = default);
}
