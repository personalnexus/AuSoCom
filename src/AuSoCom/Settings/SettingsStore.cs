using System.Text.Json;

namespace AuSoCom.Settings;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public SettingsDocument Load(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<SettingsDocument>(stream, Options) ?? new SettingsDocument();
    }

    public void Save(string path, SettingsDocument document)
    {
        var temporaryPath = path + ".tmp";
        using (var stream = File.Create(temporaryPath))
            JsonSerializer.Serialize(stream, document, Options);

        if (File.Exists(path))
            File.Replace(temporaryPath, path, null);
        else
            File.Move(temporaryPath, path);
    }
}
