using System.Diagnostics;

namespace AuSoCom.Audio;

internal sealed class SceneFadeEnvelope
{
    private FadeState _state;

    public SceneFadeEnvelope(TimeSpan? fadeIn)
    {
        if (fadeIn < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(fadeIn), "Fade-in duration cannot be negative.");

        _state = fadeIn is { } duration && duration > TimeSpan.Zero
            ? new FadeState(0, 1, Stopwatch.GetTimestamp(), duration)
            : FadeState.Completed(1);
    }

    public float Gain => Volatile.Read(ref _state).GetGain();

    public TimeSpan BeginFadeOut(TimeSpan? fadeOut)
    {
        if (fadeOut < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(fadeOut), "Fade-out duration cannot be negative.");

        if (fadeOut is not { } duration || duration == TimeSpan.Zero)
        {
            Volatile.Write(ref _state, FadeState.Completed(0));
            return TimeSpan.Zero;
        }

        var state = new FadeState(Gain, 0, Stopwatch.GetTimestamp(), duration);
        Volatile.Write(ref _state, state);
        return duration;
    }

    internal static float CalculateGain(
        float startGain,
        float targetGain,
        TimeSpan elapsed,
        TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return targetGain;

        var progress = Math.Clamp(elapsed.TotalSeconds / duration.TotalSeconds, 0, 1);
        return startGain + (targetGain - startGain) * (float)progress;
    }

    private sealed record FadeState(float StartGain, float TargetGain, long StartedAt, TimeSpan Duration)
    {
        public float GetGain() => CalculateGain(
            StartGain,
            TargetGain,
            Stopwatch.GetElapsedTime(StartedAt),
            Duration);

        public static FadeState Completed(float gain) =>
            new(gain, gain, Stopwatch.GetTimestamp(), TimeSpan.Zero);
    }
}
