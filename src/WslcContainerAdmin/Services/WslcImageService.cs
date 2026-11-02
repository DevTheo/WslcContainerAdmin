using WslcContainerAdmin.Models;

namespace WslcContainerAdmin.Services;

public sealed class WslcImageService(IWslcCli cli) : IWslcImageService
{
    public async Task<IReadOnlyList<ImageInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = await cli.RunAsync(["image", "list", "--format", "json"], cancellationToken);
        EnsureSuccess(result, "image list");
        return WslcJson.ReadLines(result.StandardOutput).Select(row => new ImageInfo(
            Id: WslcJson.Text(row, "ID"),
            Repository: WslcJson.Text(row, "Repository"),
            Tag: WslcJson.Text(row, "Tag"),
            Size: WslcJson.Text(row, "Size"),
            CreatedSince: WslcJson.Text(row, "CreatedSince"),
            Digest: WslcJson.Text(row, "Digest"),
            Containers: WslcJson.Text(row, "Containers"))).ToList();
    }

    public async Task<string> InspectAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("An image ID is required.", nameof(id));
        }

        var result = await cli.RunAsync(["image", "inspect", id.Trim()], cancellationToken);
        EnsureSuccess(result, "image inspect");
        return result.StandardOutput;
    }

    public async Task PullAsync(string image, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(image))
        {
            throw new ArgumentException("An image name is required.", nameof(image));
        }

        var result = await cli.RunAsync(["image", "pull", image.Trim()], cancellationToken);
        EnsureSuccess(result, "image pull");
    }

    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("An image ID is required.", nameof(id));
        }

        var result = await cli.RunAsync(["image", "remove", id.Trim()], cancellationToken);
        EnsureSuccess(result, "image remove");
    }

    private static void EnsureSuccess(WslcCliResult result, string command)
    {
        if (result.ExitCode != 0)
        {
            var detail = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError;
            throw new InvalidOperationException($"wslc {command} failed (exit {result.ExitCode}): {detail.Trim()}");
        }
    }
}
