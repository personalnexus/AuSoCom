using System.Diagnostics;

namespace AuSoCom.Audio;

internal sealed class SceneFadeEnvelope
{
    private FadeState _state;
    private readonly TimeSpan _fadeOut;
    private readonly long _startedAt;

    public SceneFadeEnvelope(TimeSpan? delay, TimeSpan? fadeIn, TimeSpan? fadeOut)
    {
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), "Delay cannot be negative.");
        if (fadeIn < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(fadeIn), "Fade-in duration cannot be negative.");
        if (fadeOut < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(fadeOut), "Fade-out duration cannot be negative.");

        _fadeOut = fadeOut ?? TimeSpan.Zero;
        Delay = delay ?? TimeSpan.Zero;
        _startedAt = Stopwatch.GetTimestamp();
        _state = new FadeState(
            0,
            1,
            _startedAt,
            fadeIn ?? TimeSpan.Zero,
            Delay);
    }

    public TimeSpan Delay { get; }
    public bool IsDelayed => Stopwatch.GetElapsedTime(_startedAt) < Delay;

    public float Gain => Volatile.Read(ref _state).GetGain();

    public TimeSpan BeginFadeOut()
    {
        var currentGain = Gain;
        if (currentGain <= 0 || _fadeOut == TimeSpan.Zero)
        {
            Volatile.Write(ref _state, FadeState.Completed(0));
            return TimeSpan.Zero;
        }

        var state = new FadeState(currentGain, 0, Stopwatch.GetTimestamp(), _fadeOut, TimeSpan.Zero);
        Volatile.Write(ref _state, state);
        return _fadeOut;
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

    internal static float CalculateGain(
        float startGain,
        float targetGain,
        TimeSpan elapsed,
        TimeSpan initialDelay,
        TimeSpan duration) =>
        elapsed < initialDelay
            ? startGain
            : CalculateGain(startGain, targetGain, elapsed - initialDelay, duration);

    private sealed record FadeState(
        float StartGain,
        float TargetGain,
        long StartedAt,
        TimeSpan Duration,
        TimeSpan InitialDelay)
    {
        public float GetGain()
        {
            var elapsed = Stopwatch.GetElapsedTime(StartedAt);
            return CalculateGain(StartGain, TargetGain, elapsed, InitialDelay, Duration);
        }

        public static FadeState Completed(float gain) =>
            new(gain, gain, Stopwatch.GetTimestamp(), TimeSpan.Zero, TimeSpan.Zero);
    }
}
