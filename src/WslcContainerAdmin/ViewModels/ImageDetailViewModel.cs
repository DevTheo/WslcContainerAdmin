using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslcContainerAdmin.Models;
using WslcContainerAdmin.Services;

namespace WslcContainerAdmin.ViewModels;

public partial class ImageDetailViewModel(IWslcImageService images, INavigationService navigation) : ViewModelBase
{
    private ViewModelBase? _returnTo;

    [ObservableProperty]
    private ImageInfo? _image;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _created = string.Empty;

    [ObservableProperty]
    private string _platform = string.Empty;

    public ObservableCollection<ImageDetailRow> Configuration { get; } = [];
    public ObservableCollection<string> LayerDigests { get; } = [];

    public void SetImage(ImageInfo image)
    {
        Image = image;
        _returnTo = navigation.Current;
    }

    public override async Task OnNavigatedToAsync()
    {
        if (Image is null)
        {
            return;
        }

        ErrorMessage = null;
        Configuration.Clear();
        LayerDigests.Clear();
        try
        {
            using var document = JsonDocument.Parse(await images.InspectAsync(Image.Id));
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                root = root[0];
            }

            Created = Text(root, "Created");
            Platform = $"{Text(root, "Os")} / {Text(root, "Architecture")}".Trim(' ', '/');
            AddConfig(root, "Cmd", "Command");
            AddConfig(root, "Entrypoint", "Entrypoint");
            AddConfig(root, "WorkingDir", "Working directory");
            AddConfig(root, "User", "User");
            AddObjectKeys(root, "ExposedPorts", "Exposed ports");
            AddStringArray(root, "Env", "Environment");
            AddObjectPairs(root, "Labels", "Labels");

            if (root.TryGetProperty("RepoTags", out var tags) && tags.ValueKind == JsonValueKind.Array)
            {
                AddRow("Tags", string.Join(Environment.NewLine, tags.EnumerateArray().Select(tag => tag.GetString()).Where(tag => !string.IsNullOrWhiteSpace(tag))));
            }
            if (root.TryGetProperty("RootFS", out var rootFs) && rootFs.TryGetProperty("Layers", out var layers) && layers.ValueKind == JsonValueKind.Array)
            {
                foreach (var layer in layers.EnumerateArray())
                {
                    if (layer.GetString() is { Length: > 0 } digest)
                    {
                        LayerDigests.Add(digest);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        if (_returnTo is not null)
        {
            await navigation.NavigateAsync(_returnTo);
        }
    }

    private void AddConfig(JsonElement root, string property, string title)
    {
        if (!root.TryGetProperty("Config", out var config) || !config.TryGetProperty(property, out var value))
        {
            return;
        }
        var text = Value(value);
        if (text.Length > 0)
        {
            AddRow(title, text);
        }
    }

    private void AddObjectKeys(JsonElement root, string property, string title)
    {
        if (root.TryGetProperty("Config", out var config) && config.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Object)
        {
            AddRow(title, string.Join(Environment.NewLine, value.EnumerateObject().Select(item => item.Name)));
        }
    }

    private void AddStringArray(JsonElement root, string property, string title)
    {
        if (root.TryGetProperty("Config", out var config) && config.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array)
        {
            AddRow(title, string.Join(Environment.NewLine, value.EnumerateArray().Select(item => item.GetString()).Where(item => !string.IsNullOrWhiteSpace(item))));
        }
    }

    private void AddObjectPairs(JsonElement root, string property, string title)
    {
        if (root.TryGetProperty("Config", out var config) && config.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Object)
        {
            AddRow(title, string.Join(Environment.NewLine, value.EnumerateObject().Select(item => $"{item.Name}={Value(item.Value)}")));
        }
    }

    private void AddRow(string title, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            Configuration.Add(new ImageDetailRow(title, value));
        }
    }

    private static string Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    private static string Value(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Array => string.Join(" ", value.EnumerateArray().Select(Value)),
        JsonValueKind.Object => string.Join(", ", value.EnumerateObject().Select(item => $"{item.Name}={Value(item.Value)}")),
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
        _ => value.ToString(),
    };
}

public sealed record ImageDetailRow(string Name, string Value);
