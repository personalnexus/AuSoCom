namespace AuSoCom.Settings;

public sealed class ViewSettings
{
    public Dictionary<string, WindowSettings> Windows { get; set; } = new(StringComparer.Ordinal);
}
