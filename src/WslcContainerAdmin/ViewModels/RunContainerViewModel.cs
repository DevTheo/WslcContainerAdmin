using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WslcContainerAdmin.Models;
using WslcContainerAdmin.Services;

namespace WslcContainerAdmin.ViewModels;

public partial class RunContainerViewModel(
    IWslcImageService images,
    IWslcInventoryService inventory,
    IWslcContainerService containers,
    INavigationService navigation,
    IServiceProvider services) : ViewModelBase
{
    public ObservableCollection<ImageInfo> AvailableImages { get; } = [];
    public ObservableCollection<VolumeInfo> AvailableVolumes { get; } = [];
    public ObservableCollection<int> ImageExposedPorts { get; } = [];

    [ObservableProperty]
    private bool _hasImageExposedPorts;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSetupStep))]
    [NotifyPropertyChangedFor(nameof(IsReviewStep))]
    private int _currentStep = 1;

    [ObservableProperty]
    private ImageInfo? _selectedImage;

    [ObservableProperty]
    private string _imageName = string.Empty;

    [ObservableProperty]
    private string _containerName = string.Empty;

    [ObservableProperty]
    private string _command = string.Empty;

    [ObservableProperty]
    private string _arguments = string.Empty;

    public ObservableCollection<VolumeMountItem> VolumeMounts { get; } = [];
    public ObservableCollection<PortMappingItem> PortMappings { get; } = [];

    [ObservableProperty]
    private string _environmentVariables = string.Empty;

    [ObservableProperty]
    private bool _runInBackground = true;

    [ObservableProperty]
    private bool _removeOnExit;

    [ObservableProperty]
    private bool _keepStdinOpen;

    [ObservableProperty]
    private bool _passAllGpus;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StopTimeoutSummary))]
    private string _stopTimeoutSeconds = string.Empty;

    [ObservableProperty]
    private string _entrypoint = string.Empty;
    [ObservableProperty]
    private string _workingDirectory = string.Empty;
    [ObservableProperty]
    private string _userName = string.Empty;
    [ObservableProperty]
    private string _consoleMode = "None";
    [ObservableProperty]
    private NetworkInfo? _selectedNetwork;
    [ObservableProperty]
    private string _hostname = string.Empty;
    [ObservableProperty]
    private string _domainName = string.Empty;
    [ObservableProperty]
    private string _ipv4Address = string.Empty;
    [ObservableProperty]
    private string _dnsServers = string.Empty;
    [ObservableProperty]
    private string _environmentFile = string.Empty;
    [ObservableProperty]
    private string _labels = string.Empty;
    [ObservableProperty]
    private string _memoryLimit = string.Empty;
    [ObservableProperty]
    private string _cpuLimit = string.Empty;
    [ObservableProperty]
    private string _sharedMemorySize = string.Empty;
    [ObservableProperty]
    private string? _statusMessage;

    public ObservableCollection<NetworkInfo> AvailableNetworks { get; } = [];
    public IReadOnlyList<string> ConsoleModes { get; } = ["None", "Interactive (-i)", "TTY (-t)", "Interactive + TTY (-i -t)"];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public bool IsSetupStep => CurrentStep == 1;
    public bool IsReviewStep => CurrentStep == 2;

    public string StopTimeoutSummary => string.IsNullOrWhiteSpace(StopTimeoutSeconds)
        ? "WSLc default"
        : $"{StopTimeoutSeconds} seconds";

    public override async Task OnNavigatedToAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var rows = await images.ListAsync();
            AvailableImages.Clear();
            foreach (var row in rows)
            {
                AvailableImages.Add(row);
            }

            var volumeRows = await inventory.ListVolumesAsync();
            AvailableVolumes.Clear();
            foreach (var volume in volumeRows)
            {
                AvailableVolumes.Add(volume);
            }

            var networkRows = await inventory.ListNetworksAsync();
            AvailableNetworks.Clear();
            foreach (var network in networkRows)
            {
                AvailableNetworks.Add(network);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SetImage(string image) => ImageName = image;

    [RelayCommand]
    private async Task UseSelectedImageAsync()
    {
        if (SelectedImage is not null)
        {
            ImageName = $"{SelectedImage.Repository}:{SelectedImage.Tag}";
        }

        await LoadExposedPortsAsync();
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(ImageName))
        {
            ErrorMessage = "Select or pull an image before continuing.";
            return;
        }

        await LoadExposedPortsAsync();
        CurrentStep = 2;
    }

    [RelayCommand]
    private void Previous() => CurrentStep = 1;

    [RelayCommand]
    private async Task PullImageAsync()
    {
        if (string.IsNullOrWhiteSpace(ImageName))
        {
            ErrorMessage = "Enter an image name to pull.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = null;
        try
        {
            await images.PullAsync(ImageName.Trim());
            var rows = await images.ListAsync();
            AvailableImages.Clear();
            foreach (var image in rows)
            {
                AvailableImages.Add(image);
            }
            StatusMessage = $"Pulled {ImageName.Trim()}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
    [RelayCommand]
    private void AddVolumeMount() => VolumeMounts.Add(new VolumeMountItem());

    [RelayCommand]
    private void RemoveVolumeMount(VolumeMountItem item) => VolumeMounts.Remove(item);

    [RelayCommand]
    private void AddPortMapping() => PortMappings.Add(new PortMappingItem());

    [RelayCommand]
    private void RemovePortMapping(PortMappingItem item) => PortMappings.Remove(item);

    [RelayCommand]
    private void AddExposedPortMapping(int port) => PortMappings.Add(new PortMappingItem { ContainerPort = port });


    [RelayCommand]
    private async Task LaunchAsync()
    {
        if (string.IsNullOrWhiteSpace(ImageName))
        {
            CurrentStep = 1;
            ErrorMessage = "Select an image or enter an image name.";
            return;
        }

        if (VolumeMounts.Any(mount => mount.SelectedVolume is null || string.IsNullOrWhiteSpace(mount.ContainerPath)))
        {
            ErrorMessage = "Select a volume and enter its container path for each volume mount.";
            return;
        }

        if (PortMappings.Any(mapping => mapping.HostPort <= 0 || mapping.ContainerPort <= 0))
        {
            ErrorMessage = "Enter a positive host and container port for each port mapping.";
            return;
        }

        int? stopTimeoutSeconds = null;
        if (!string.IsNullOrWhiteSpace(StopTimeoutSeconds))
        {
            if (!int.TryParse(StopTimeoutSeconds, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeout) || timeout < -1)
            {
                ErrorMessage = "Stop timeout must be -1 or a non-negative number of seconds.";
                return;
            }

            stopTimeoutSeconds = timeout;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var consoleMode = ConsoleMode;
            var options = new ContainerRunOptions(
                ImageName.Trim(),
                ContainerName,
                Command,
                SplitLines(Arguments),
                PortMappings.Select(mapping => new PortMapping(mapping.HostPort, mapping.ContainerPort)).ToList(),
                VolumeMounts.Select(mount => new VolumeMount(mount.SelectedVolume!.Name, mount.ContainerPath)).ToList(),
                SplitLines(EnvironmentVariables),
                RunInBackground,
                RemoveOnExit,
                consoleMode.Contains("Interactive", StringComparison.Ordinal),
                PassAllGpus,
                stopTimeoutSeconds,
                Entrypoint,
                WorkingDirectory,
                UserName,
                consoleMode.Contains("TTY", StringComparison.Ordinal),
                SelectedNetwork?.Name,
                Hostname,
                DomainName,
                Ipv4Address,
                SplitLines(DnsServers),
                EnvironmentFile,
                SplitLines(Labels),
                MemoryLimit,
                CpuLimit,
                SharedMemorySize);
            await containers.RunAsync(options);
            var list = services.GetRequiredService<ContainersViewModel>();
            await navigation.NavigateAsync(list);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        var list = services.GetRequiredService<ContainersViewModel>();
        await navigation.NavigateAsync(list);
    }

    private async Task LoadExposedPortsAsync()
    {
        ImageExposedPorts.Clear();
        HasImageExposedPorts = false;
        if (SelectedImage is null)
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(await images.InspectAsync(SelectedImage.Id));
            var inspectedImage = document.RootElement;
            if (inspectedImage.ValueKind == JsonValueKind.Array)
            {
                inspectedImage = inspectedImage[0];
            }

            var exposedPorts = inspectedImage.GetProperty("Config").GetProperty("ExposedPorts");
            foreach (var property in exposedPorts.EnumerateObject())
            {
                var separator = property.Name.IndexOf('/');
                if (separator > 0 && int.TryParse(property.Name.AsSpan(0, separator), NumberStyles.None, CultureInfo.InvariantCulture, out var port))
                {
                    ImageExposedPorts.Add(port);
                }
            }

            HasImageExposedPorts = ImageExposedPorts.Count > 0;
        }
        catch
        {
            ImageExposedPorts.Clear();
            HasImageExposedPorts = false;
        }
    }


    private static IReadOnlyList<string> SplitLines(string value) =>
        value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public partial class VolumeMountItem : ObservableObject
{
    [ObservableProperty]
    private VolumeInfo? _selectedVolume;

    [ObservableProperty]
    private string _containerPath = string.Empty;
}

public partial class PortMappingItem : ObservableObject
{
    [ObservableProperty]
    private int _hostPort;

    [ObservableProperty]
    private int _containerPort;
}
