using AuSoCom.Domain;

namespace AuSoCom.Audio;

public readonly record struct AudioDeviceDescriptor(
    string Id,
    string Name,
    bool IsInput,
    string LegacyId = "");

public interface IAudioEngine : IDisposable
{
    IReadOnlyList<AudioDeviceDescriptor> GetInputDevices();
    IReadOnlyList<AudioDeviceDescriptor> GetOutputDevices();
    IReadOnlyList<string> GetAsioDrivers();
    void Play(MediaFile file, string? outputDeviceId = null);
    void StartScene(Scene scene, IReadOnlyList<MediaFile> files, IReadOnlyList<Device> devices);
    Task StopAsync(TimeSpan? fadeOut);
    void Stop();
}
