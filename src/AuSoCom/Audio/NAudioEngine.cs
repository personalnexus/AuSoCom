using AuSoCom.Domain;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Diagnostics;

namespace AuSoCom.Audio;

public sealed class NAudioEngine : IAudioEngine
{
    private const int SceneSampleRate = 44100;
    private static readonly WaveFormat SceneWaveFormat =
        WaveFormat.CreateIeeeFloatWaveFormat(SceneSampleRate, 2);

    private readonly object _gate = new();
    private readonly List<IWavePlayer> _scenePlayers = [];
    private readonly List<CaptureSession> _sceneCaptures = [];
    private readonly List<IDisposable> _sceneResources = [];
    private SceneFadeEnvelope? _sceneFade;
    private long _sceneOperationId;
    private IWavePlayer? _player;
    private AudioFileReader? _reader;

    public IReadOnlyList<AudioDeviceDescriptor> GetInputDevices() =>
        GetDevices(DataFlow.Capture, isInput: true);

    public IReadOnlyList<AudioDeviceDescriptor> GetOutputDevices() =>
        GetDevices(DataFlow.Render, isInput: false);

    public IReadOnlyList<string> GetAsioDrivers() =>
        OperatingSystem.IsWindows() ? AsioOut.GetDriverNames() : [];

    private static IReadOnlyList<AudioDeviceDescriptor> GetDevices(DataFlow flow, bool isInput)
    {
        if (!OperatingSystem.IsWindows()) return [];

        using var enumerator = new MMDeviceEnumerator();
        var endpoints = enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active);
        var descriptors = new List<AudioDeviceDescriptor>();
        for (var index = 0; index < endpoints.Count; index++)
        {
            var endpoint = endpoints[index];
            descriptors.Add(new AudioDeviceDescriptor(endpoint.ID, endpoint.FriendlyName, isInput, index.ToString()));
            endpoint.Dispose();
        }
        return descriptors;
    }

    public void Play(MediaFile file, string? outputDeviceId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file.Path);
        if (!File.Exists(file.Path))
            throw new FileNotFoundException("The selected media file does not exist.", file.Path);

        Stop();

        lock (_gate)
        {
            _reader = new AudioFileReader(file.Path);
            if (file.StartTime is { } start)
                _reader.CurrentTime = start;

            _player = TryCreateAsioPlayer(outputDeviceId)
                ?? new WaveOutEvent();
            _player.Init(_reader);
            _player.Play();
        }
    }

    public void StartScene(Scene scene, IReadOnlyList<MediaFile> files, IReadOnlyList<Device> devices)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(devices);
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live scene audio is currently supported on Windows only.");

        Stop();

        try
        {
            var fadeEnvelope = new SceneFadeEnvelope(scene.FadeIn);
            lock (_gate)
                _sceneFade = fadeEnvelope;

            var sceneFiles = files.Where(file => scene.InputIds.Contains(file.Id)).ToArray();
            var sceneInputs = devices
                .Where(device => scene.InputIds.Contains(device.Id) &&
                                 device.Role is DeviceRole.InputStereo or DeviceRole.InputLeft or DeviceRole.InputRight)
                .ToArray();
            var sceneOutputs = devices
                .Where(device => scene.OutputIds.Contains(device.Id) &&
                                 device.Role is DeviceRole.OutputStereo or DeviceRole.OutputLeft or DeviceRole.OutputRight)
                .ToArray();
            if (sceneOutputs.Length == 0)
                throw new InvalidOperationException("The scene has no available output devices.");

            var outputMixers = new Dictionary<string, MixingSampleProvider>(StringComparer.Ordinal);

            MixingSampleProvider GetOutputMixer(Device output)
            {
                var deviceId = GetDeviceId(output);
                if (!outputMixers.TryGetValue(deviceId, out var mixer))
                {
                    mixer = new MixingSampleProvider(SceneWaveFormat) { ReadFully = true };
                    outputMixers.Add(deviceId, mixer);
                }
                return mixer;
            }

            foreach (var output in sceneOutputs)
                GetOutputMixer(output);

            foreach (var mediaFile in sceneFiles)
            {
                foreach (var output in sceneOutputs)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(mediaFile.Path);
                    if (!File.Exists(mediaFile.Path))
                        throw new FileNotFoundException("A scene media file does not exist.", mediaFile.Path);

                    var reader = new AudioFileReader(mediaFile.Path);
                    _sceneResources.Add(reader);
                    var start = mediaFile.StartTime ?? TimeSpan.Zero;
                    var end = mediaFile.EndTime;
                    if (end is { } endTime && endTime <= start)
                        throw new InvalidOperationException($"End time must be after start time for '{mediaFile.ShortName}'.");
                    reader.CurrentTime = start;

                    ISampleProvider source = new WdlResamplingSampleProvider(reader, SceneSampleRate);
                    source = new RoutedSampleProvider(
                        source,
                        DeviceRole.InputStereo,
                        output.Role,
                        () => fadeEnvelope.Gain * GetRouteGain(scene, mediaFile.Id, output.Id));
                    if (end is { } limit)
                        source = new LimitedSampleProvider(source, (limit - start).TotalSeconds);
                    GetOutputMixer(output).AddMixerInput(source);
                }
            }

            var captureSessions = new Dictionary<string, CaptureSession>(StringComparer.Ordinal);
            foreach (var input in sceneInputs)
            {
                foreach (var output in sceneOutputs)
                {
                    var inputDeviceId = GetDeviceId(input);
                    if (!captureSessions.TryGetValue(inputDeviceId, out var session))
                    {
                        var endpoint = ResolveEndpoint(inputDeviceId, DataFlow.Capture);
                        var capture = new WasapiCapture(endpoint);
                        session = new CaptureSession(endpoint, capture);
                        capture.DataAvailable += (_, eventArgs) => session.AddSamples(eventArgs.Buffer, eventArgs.BytesRecorded);
                        captureSessions.Add(inputDeviceId, session);
                        _sceneCaptures.Add(session);
                    }

                    var buffer = new BufferedWaveProvider(session.Capture.WaveFormat)
                    {
                        BufferDuration = TimeSpan.FromSeconds(2),
                        DiscardOnBufferOverflow = true
                    };
                    session.AddRouteBuffer(buffer);
                    ISampleProvider source = new WaveToSampleProvider(buffer);
                    source = new WdlResamplingSampleProvider(source, SceneSampleRate);
                    source = new RoutedSampleProvider(
                        source,
                        input.Role,
                        output.Role,
                        () => fadeEnvelope.Gain * GetRouteGain(scene, input.Id, output.Id));
                    GetOutputMixer(output).AddMixerInput(source);
                }
            }

            foreach (var (deviceId, mixer) in outputMixers)
            {
                var endpoint = ResolveEndpoint(deviceId, DataFlow.Render);
                var player = new WasapiOut(endpoint, AudioClientShareMode.Shared, true, 100);
                _sceneResources.Add(endpoint);
                _scenePlayers.Add(player);
                player.Init(new SampleToWaveProvider(mixer));
                player.Play();
            }

            foreach (var capture in _sceneCaptures)
                capture.StartRecording();
        }
        catch
        {
            Stop();
            throw;
        }
    }

    public async Task StopAsync(TimeSpan? fadeOut)
    {
        SceneFadeEnvelope? fadeEnvelope;
        long operationId;
        TimeSpan duration;
        lock (_gate)
        {
            fadeEnvelope = _sceneFade;
            if (fadeEnvelope is null)
            {
                Stop();
                return;
            }

            duration = fadeEnvelope.BeginFadeOut(fadeOut);
            operationId = ++_sceneOperationId;
        }

        if (duration > TimeSpan.Zero)
            await DelayFor(duration).ConfigureAwait(false);

        lock (_gate)
        {
            if (_sceneOperationId == operationId && ReferenceEquals(_sceneFade, fadeEnvelope))
                Stop();
        }
    }

    private static async Task DelayFor(TimeSpan duration)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < duration)
        {
            var remaining = duration - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero)
                break;
            await Task.Delay(remaining > TimeSpan.FromDays(20) ? TimeSpan.FromDays(20) : remaining)
                .ConfigureAwait(false);
        }
    }

    private static string GetDeviceId(Device device) =>
        string.IsNullOrWhiteSpace(device.DeviceId) ? device.SystemName : device.DeviceId;

    private static float GetRouteGain(Scene scene, Guid inputId, Guid outputId)
    {
        if (scene.IsInputMuted(inputId) || scene.IsOutputMuted(outputId))
            return 0;

        return (float)(
            scene.GetRouting(inputId, outputId) / 100 *
            scene.GetInputVolume(inputId) / 100 *
            scene.GetOutputVolume(outputId) / 100);
    }

    private static MMDevice ResolveEndpoint(string deviceId, DataFlow flow)
    {
        using var enumerator = new MMDeviceEnumerator();
        if (int.TryParse(deviceId, out var index))
        {
            var endpoints = enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active);
            if (index >= 0 && index < endpoints.Count)
                return endpoints[index];
        }

        return enumerator.GetDevice(deviceId);
    }

    private static IWavePlayer? TryCreateAsioPlayer(string? outputDeviceId)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(outputDeviceId)) return null;

        var driverNames = AsioOut.GetDriverNames();
        return driverNames.Contains(outputDeviceId, StringComparer.Ordinal)
            ? new AsioOut(outputDeviceId)
            : null;
    }

    public void Stop()
    {
        lock (_gate)
        {
            _sceneOperationId++;
            _sceneFade = null;

            foreach (var player in _scenePlayers)
            {
                player.Stop();
                player.Dispose();
            }
            _scenePlayers.Clear();

            foreach (var capture in _sceneCaptures)
                capture.Dispose();
            _sceneCaptures.Clear();

            foreach (var resource in _sceneResources.AsEnumerable().Reverse())
                resource.Dispose();
            _sceneResources.Clear();

            _player?.Stop();
            _player?.Dispose();
            _player = null;
            _reader?.Dispose();
            _reader = null;
        }
    }

    public void Dispose() => Stop();

    private sealed class CaptureSession(MMDevice endpoint, WasapiCapture capture) : IDisposable
    {
        private readonly List<BufferedWaveProvider> _routeBuffers = [];
        private readonly object _gate = new();
        private bool _isRecording;

        public WasapiCapture Capture { get; } = capture;

        public void AddRouteBuffer(BufferedWaveProvider buffer)
        {
            lock (_gate)
                _routeBuffers.Add(buffer);
        }

        public void AddSamples(byte[] data, int length)
        {
            lock (_gate)
            {
                foreach (var buffer in _routeBuffers)
                    buffer.AddSamples(data, 0, length);
            }
        }

        public void StartRecording()
        {
            Capture.StartRecording();
            _isRecording = true;
        }

        public void Dispose()
        {
            if (_isRecording)
                Capture.StopRecording();
            Capture.Dispose();
            endpoint.Dispose();
        }
    }

    private sealed class RoutedSampleProvider(
        ISampleProvider source,
        DeviceRole inputRole,
        DeviceRole outputRole,
        Func<float> getGain) : ISampleProvider
    {
        private float[] _sourceBuffer = [];

        public WaveFormat WaveFormat => SceneWaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            var requestedFrames = count / 2;
            var sourceChannels = source.WaveFormat.Channels;
            var sourceSamplesNeeded = requestedFrames * sourceChannels;
            if (_sourceBuffer.Length < sourceSamplesNeeded)
                Array.Resize(ref _sourceBuffer, sourceSamplesNeeded);
            var samplesRead = source.Read(_sourceBuffer, 0, sourceSamplesNeeded);
            var framesRead = samplesRead / sourceChannels;
            var gain = getGain();

            for (var frame = 0; frame < framesRead; frame++)
            {
                var sampleIndex = frame * sourceChannels;
                var left = _sourceBuffer[sampleIndex];
                var right = sourceChannels > 1 ? _sourceBuffer[sampleIndex + 1] : left;

                if (inputRole == DeviceRole.InputLeft)
                    right = left;
                else if (inputRole == DeviceRole.InputRight)
                    left = right;

                var outputIndex = offset + frame * 2;
                if (outputRole == DeviceRole.OutputLeft)
                {
                    buffer[outputIndex] = (left + right) * 0.5f * gain;
                    buffer[outputIndex + 1] = 0;
                }
                else if (outputRole == DeviceRole.OutputRight)
                {
                    buffer[outputIndex] = 0;
                    buffer[outputIndex + 1] = (left + right) * 0.5f * gain;
                }
                else
                {
                    buffer[outputIndex] = left * gain;
                    buffer[outputIndex + 1] = right * gain;
                }
            }

            return framesRead * 2;
        }
    }

    private sealed class LimitedSampleProvider(ISampleProvider source, double durationSeconds) : ISampleProvider
    {
        private long _remainingSamples = (long)(durationSeconds * source.WaveFormat.SampleRate * source.WaveFormat.Channels);

        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            if (_remainingSamples <= 0) return 0;
            var read = source.Read(buffer, offset, (int)Math.Min(count, _remainingSamples));
            _remainingSamples -= read;
            return read;
        }
    }
}
