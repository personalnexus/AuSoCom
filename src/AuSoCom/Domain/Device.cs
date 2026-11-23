namespace AuSoCom.Domain;

public sealed class Device : NamedElement
{
    public string DeviceId { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public DeviceRole Role { get; set; }
    public bool Favorite { get; set; }
    public bool AutoAdd { get; set; }
}
