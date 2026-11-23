namespace AuSoCom.Domain;

public sealed class MediaFile : NamedElement
{
    public string Path { get; set; } = string.Empty;
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
}
