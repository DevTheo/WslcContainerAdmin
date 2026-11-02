using System.Text.Json;

namespace WslcContainerAdmin.Services;

internal static class WslcJson
{
    public static List<JsonElement> ReadLines(string output)
    {
        var rows = new List<JsonElement>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            using var document = JsonDocument.Parse(line);
            rows.Add(document.RootElement.Clone());
        }

        return rows;
    }

    public static string Text(JsonElement row, string property) =>
        row.TryGetProperty(property, out var value) && value.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined
            ? value.ToString()
            : string.Empty;
}
