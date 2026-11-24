using NAudio.Wave;

namespace AuSoCom.Audio;

internal sealed class DelayedSampleProvider(
    ISampleProvider source,
    SceneFadeEnvelope fadeEnvelope,
    bool consumeSourceDuringDelay) : ISampleProvider
{
    private float[] _discardBuffer = [];

    public WaveFormat WaveFormat => source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        if (!fadeEnvelope.IsDelayed)
            return source.Read(buffer, offset, count);

        if (consumeSourceDuringDelay)
        {
            if (_discardBuffer.Length < count)
                Array.Resize(ref _discardBuffer, count);
            source.Read(_discardBuffer, 0, count);
        }

        Array.Clear(buffer, offset, count);
        return count;
    }
}
