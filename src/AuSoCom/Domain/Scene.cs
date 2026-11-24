using System.Text.Json.Serialization;

namespace AuSoCom.Domain;

public sealed class Scene : NamedElement, IJsonOnDeserialized
{
    public bool AutoStart { get; set; }
    public List<Guid> InputIds { get; set; } = [];
    public List<Guid> OutputIds { get; set; } = [];
    public Dictionary<string, double> Routing { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, double> InputVolumes { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, double> OutputVolumes { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, double> InputGainsDb { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, double> OutputGainsDb { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, TimeSpan?> InputDelays { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, TimeSpan?> InputFadeIns { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, TimeSpan?> InputFadeOuts { get; set; } = new(StringComparer.Ordinal);
    public List<Guid> MutedInputIds { get; set; } = [];
    public List<Guid> MutedOutputIds { get; set; } = [];

    [JsonPropertyName("Delay")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TimeSpan? LegacyDelay { get; set; }

    [JsonPropertyName("FadeIn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TimeSpan? LegacyFadeIn { get; set; }

    [JsonPropertyName("FadeOut")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TimeSpan? LegacyFadeOut { get; set; }

    public double GetRouting(Guid inputId, Guid outputId) =>
        Routing.TryGetValue(RoutingKey(inputId, outputId), out var amount) ? amount : 100;

    public void SetRouting(Guid inputId, Guid outputId, double amount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(amount, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(amount, 100);
        Routing[RoutingKey(inputId, outputId)] = amount;
    }

    public static string RoutingKey(Guid inputId, Guid outputId) => $"{inputId:N}:{outputId:N}";

    public double GetInputVolume(Guid inputId) => GetVolume(InputVolumes, inputId);

    public void SetInputVolume(Guid inputId, double volume) => SetVolume(InputVolumes, inputId, volume);

    public bool IsInputMuted(Guid inputId) => MutedInputIds.Contains(inputId);

    public void SetInputMuted(Guid inputId, bool isMuted) => SetMuted(MutedInputIds, inputId, isMuted);

    public double GetOutputVolume(Guid outputId) => GetVolume(OutputVolumes, outputId);

    public void SetOutputVolume(Guid outputId, double volume) => SetVolume(OutputVolumes, outputId, volume);

    public double GetInputGainDb(Guid inputId) => GetGainDb(InputGainsDb, inputId);

    public void SetInputGainDb(Guid inputId, double gainDb) => SetGainDb(InputGainsDb, inputId, gainDb);

    public double GetOutputGainDb(Guid outputId) => GetGainDb(OutputGainsDb, outputId);

    public void SetOutputGainDb(Guid outputId, double gainDb) => SetGainDb(OutputGainsDb, outputId, gainDb);

    public TimeSpan? GetInputDelay(Guid inputId) => GetInputDuration(InputDelays, inputId);

    public void SetInputDelay(Guid inputId, TimeSpan? delay) => SetInputDuration(InputDelays, inputId, delay);

    public TimeSpan? GetInputFadeIn(Guid inputId) => GetInputDuration(InputFadeIns, inputId);

    public void SetInputFadeIn(Guid inputId, TimeSpan? fadeIn) => SetInputDuration(InputFadeIns, inputId, fadeIn);

    public TimeSpan? GetInputFadeOut(Guid inputId) => GetInputDuration(InputFadeOuts, inputId);

    public void SetInputFadeOut(Guid inputId, TimeSpan? fadeOut) => SetInputDuration(InputFadeOuts, inputId, fadeOut);

    public bool IsOutputMuted(Guid outputId) => MutedOutputIds.Contains(outputId);

    public void SetOutputMuted(Guid outputId, bool isMuted) => SetMuted(MutedOutputIds, outputId, isMuted);

    public void OnDeserialized()
    {
        foreach (var inputId in InputIds)
        {
            var key = inputId.ToString("N");
            if (LegacyDelay is { } delay && !InputDelays.ContainsKey(key))
                SetInputDelay(inputId, delay);
            if (LegacyFadeIn is { } fadeIn && !InputFadeIns.ContainsKey(key))
                SetInputFadeIn(inputId, fadeIn);
            if (LegacyFadeOut is { } fadeOut && !InputFadeOuts.ContainsKey(key))
                SetInputFadeOut(inputId, fadeOut);
        }

        LegacyDelay = null;
        LegacyFadeIn = null;
        LegacyFadeOut = null;
    }

    private static double GetVolume(Dictionary<string, double> volumes, Guid elementId) =>
        volumes.TryGetValue(elementId.ToString("N"), out var volume) ? volume : 100;

    private static void SetVolume(Dictionary<string, double> volumes, Guid elementId, double volume)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(volume, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(volume, 100);
        volumes[elementId.ToString("N")] = volume;
    }

    private static double GetGainDb(Dictionary<string, double> gains, Guid elementId) =>
        gains.TryGetValue(elementId.ToString("N"), out var gainDb) ? gainDb : 0;

    private static void SetGainDb(Dictionary<string, double> gains, Guid elementId, double gainDb)
    {
        if (!double.IsFinite(gainDb) || gainDb is < -50 or > 50)
            throw new ArgumentOutOfRangeException(nameof(gainDb), gainDb, "Gain must be between -50 and +50 dB.");

        gains[elementId.ToString("N")] = gainDb;
    }

    private static TimeSpan? GetInputDuration(
        Dictionary<string, TimeSpan?> durations,
        Guid inputId) =>
        durations.TryGetValue(inputId.ToString("N"), out var duration) ? duration : null;

    private static void SetInputDuration(
        Dictionary<string, TimeSpan?> durations,
        Guid inputId,
        TimeSpan? duration)
    {
        if (duration is { } value && value < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration cannot be negative.");

        var key = inputId.ToString("N");
        if (duration is null)
            durations.Remove(key);
        else
            durations[key] = duration;
    }

    private static void SetMuted(List<Guid> mutedIds, Guid elementId, bool isMuted)
    {
        if (isMuted && !mutedIds.Contains(elementId))
            mutedIds.Add(elementId);
        else if (!isMuted)
            mutedIds.Remove(elementId);
    }
}
