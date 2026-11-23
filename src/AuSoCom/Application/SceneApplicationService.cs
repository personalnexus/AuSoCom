using AuSoCom.Domain;

namespace AuSoCom.Application;

public sealed class SceneApplicationService
{
    public Scene CreateScene(string shortName, IEnumerable<Device> devices)
    {
        var autoAddedDevices = devices.Where(device => device.AutoAdd).ToArray();
        return new Scene
        {
            ShortName = shortName,
            InputIds = autoAddedDevices
                .Where(device => device.Role is DeviceRole.InputStereo or DeviceRole.InputLeft or DeviceRole.InputRight)
                .Select(device => device.Id)
                .Distinct()
                .ToList(),
            OutputIds = GetAutoAddedOutputIds(autoAddedDevices)
        };
    }

    public Scene CreateSceneForFiles(string shortName, IEnumerable<Device> devices, IReadOnlyList<MediaFile> files)
    {
        var autoAddedDevices = devices.Where(device => device.AutoAdd).ToArray();
        return new Scene
        {
            ShortName = shortName,
            InputIds = files.Select(file => file.Id).Distinct().ToList(),
            OutputIds = GetAutoAddedOutputIds(autoAddedDevices)
        };
    }

    private static List<Guid> GetAutoAddedOutputIds(IEnumerable<Device> devices) =>
        devices
            .Where(device => device.Role is DeviceRole.OutputStereo or DeviceRole.OutputLeft or DeviceRole.OutputRight)
            .Select(device => device.Id)
            .Distinct()
            .ToList();

    public Scene Duplicate(Scene source)
    {
        var copy = new Scene
        {
            Id = Guid.NewGuid(),
            ShortName = $"{source.ShortName} copy",
            FadeIn = source.FadeIn,
            FadeOut = source.FadeOut,
            Delay = source.Delay,
            AutoStart = source.AutoStart,
            InputIds = [.. source.InputIds],
            OutputIds = [.. source.OutputIds],
            Routing = new Dictionary<string, double>(source.Routing, StringComparer.Ordinal),
            InputVolumes = new Dictionary<string, double>(source.InputVolumes, StringComparer.Ordinal),
            OutputVolumes = new Dictionary<string, double>(source.OutputVolumes, StringComparer.Ordinal),
            MutedInputIds = [.. source.MutedInputIds],
            MutedOutputIds = [.. source.MutedOutputIds]
        };
        return copy;
    }

    public void AddElement(Scene scene, Guid elementId, bool isInput)
    {
        var elements = isInput ? scene.InputIds : scene.OutputIds;
        if (!elements.Contains(elementId))
            elements.Add(elementId);
    }

    public void RemoveElement(Scene scene, Guid elementId)
    {
        scene.InputIds.Remove(elementId);
        scene.OutputIds.Remove(elementId);
        scene.InputVolumes.Remove(elementId.ToString("N"));
        scene.OutputVolumes.Remove(elementId.ToString("N"));
        scene.MutedInputIds.Remove(elementId);
        scene.MutedOutputIds.Remove(elementId);
        var elementKey = elementId.ToString("N");
        foreach (var key in scene.Routing.Keys
                     .Where(key => key.Contains(elementKey, StringComparison.Ordinal))
                     .ToArray())
            scene.Routing.Remove(key);
    }
}
