using System.Text.Json;

namespace AuSoCom.Settings;

public sealed class ApplicationSettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _path;

    public ApplicationSettingsStore(string? path = null)
    {
        _path = path ?? Path.Combine(AppContext.BaseDirectory, "AuSoCom.appsettings.json");
    }

    public ApplicationSettingsDocument Load()
    {
        if (!File.Exists(_path))
            return new ApplicationSettingsDocument();

        using var stream = File.OpenRead(_path);
        var settings = JsonSerializer.Deserialize<ApplicationSettingsDocument>(stream, Options)
            ?? throw new InvalidDataException("Application settings file is empty.");
        settings.NormalizeRecentFiles();
        return settings;
    }

    public void Save(ApplicationSettingsDocument settings)
    {
        settings.NormalizeRecentFiles();
        var temporaryPath = _path + ".tmp";
        using (var stream = File.Create(temporaryPath))
            JsonSerializer.Serialize(stream, settings, Options);

        if (File.Exists(_path))
            File.Replace(temporaryPath, _path, null);
        else
            File.Move(temporaryPath, _path);
    }
}
