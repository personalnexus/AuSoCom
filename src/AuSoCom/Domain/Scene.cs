namespace AuSoCom.Domain;

public sealed class Scene : NamedElement
{
    public TimeSpan? FadeIn { get; set; }
    public TimeSpan? FadeOut { get; set; }
    public TimeSpan Delay { get; set; }
    public bool AutoStart { get; set; }
    public List<Guid> InputIds { get; set; } = [];
    public List<Guid> OutputIds { get; set; } = [];
    public Dictionary<string, double> Routing { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, double> InputVolumes { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, double> OutputVolumes { get; set; } = new(StringComparer.Ordinal);
    public List<Guid> MutedInputIds { get; set; } = [];
    public List<Guid> MutedOutputIds { get; set; } = [];

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

    public bool IsOutputMuted(Guid outputId) => MutedOutputIds.Contains(outputId);

    public void SetOutputMuted(Guid outputId, bool isMuted) => SetMuted(MutedOutputIds, outputId, isMuted);

    private static double GetVolume(Dictionary<string, double> volumes, Guid elementId) =>
        volumes.TryGetValue(elementId.ToString("N"), out var volume) ? volume : 100;

    private static void SetVolume(Dictionary<string, double> volumes, Guid elementId, double volume)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(volume, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(volume, 100);
        volumes[elementId.ToString("N")] = volume;
    }

    private static void SetMuted(List<Guid> mutedIds, Guid elementId, bool isMuted)
    {
        if (isMuted && !mutedIds.Contains(elementId))
            mutedIds.Add(elementId);
        else if (!isMuted)
            mutedIds.Remove(elementId);
    }
}
