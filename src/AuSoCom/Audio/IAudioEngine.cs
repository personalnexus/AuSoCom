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
    TimeSpan GetFileDuration(MediaFile file);
    void Play(MediaFile file, string? outputDeviceId = null);
    TimeSpan? GetFilePlaybackPosition();
    bool IsFilePlaybackPaused { get; }
    void PauseFilePlayback();
    void ResumeFilePlayback();
    void SeekFilePlayback(TimeSpan position);
    void StopFilePlayback();
    void StartScene(Scene scene, IReadOnlyList<MediaFile> files, IReadOnlyList<Device> devices);
    Task StopAsync();
    void Stop();
}
