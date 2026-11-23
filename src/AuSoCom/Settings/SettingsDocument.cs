using AuSoCom.Domain;

namespace AuSoCom.Settings;

public sealed class SettingsDocument
{
    public List<MediaFile> Files { get; set; } = [];
    public List<Device> Devices { get; set; } = [];
    public List<Scene> Scenes { get; set; } = [];
    public Show Show { get; set; } = new();
    public ViewSettings View { get; set; } = new();
}
