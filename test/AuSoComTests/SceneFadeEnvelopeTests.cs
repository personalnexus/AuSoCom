using AuSoCom.Audio;
using FluentAssertions;
using NAudio.Wave;

namespace AuSoComTests;

[TestClass]
public sealed class SceneFadeEnvelopeTests
{
    [TestMethod]
    public void FadeInRampsFromSilenceToFullGain()
    {
        SceneFadeEnvelope.CalculateGain(0, 1, TimeSpan.Zero, TimeSpan.FromSeconds(2)).Should().Be(0);
        SceneFadeEnvelope.CalculateGain(0, 1, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)).Should().Be(0.5f);
        SceneFadeEnvelope.CalculateGain(0, 1, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)).Should().Be(1);
    }

    [TestMethod]
    public void FadeOutRampsCurrentGainToSilence()
    {
        SceneFadeEnvelope.CalculateGain(0.8f, 0, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)).Should().Be(0.4f);
        SceneFadeEnvelope.CalculateGain(0.8f, 0, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(2)).Should().Be(0);
    }

    [TestMethod]
    public void InputFadeInStartsAfterItsDelay()
    {
        SceneFadeEnvelope.CalculateGain(
            0,
            1,
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2)).Should().Be(0);
        SceneFadeEnvelope.CalculateGain(
            0,
            1,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2)).Should().Be(0.5f);
    }

    [TestMethod]
    public void DelayedFileInputDoesNotConsumeSamplesBeforeActivation()
    {
        var source = new CountingSampleProvider();
        var envelope = new SceneFadeEnvelope(TimeSpan.FromMinutes(1), null, null);
        var provider = new DelayedSampleProvider(source, envelope, consumeSourceDuringDelay: false);
        var buffer = new float[4];

        provider.Read(buffer, 0, buffer.Length);

        source.ReadCount.Should().Be(0);
        buffer.Should().OnlyContain(sample => sample == 0);
    }

    [TestMethod]
    public void DelayedLiveInputDiscardsSamplesInsteadOfBufferingThemForLater()
    {
        var source = new CountingSampleProvider();
        var envelope = new SceneFadeEnvelope(TimeSpan.FromMinutes(1), null, null);
        var provider = new DelayedSampleProvider(source, envelope, consumeSourceDuringDelay: true);
        var buffer = new float[4];

        provider.Read(buffer, 0, buffer.Length);

        source.ReadCount.Should().Be(1);
        buffer.Should().OnlyContain(sample => sample == 0);
    }

    private sealed class CountingSampleProvider : ISampleProvider
    {
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
        public int ReadCount { get; private set; }

        public int Read(float[] buffer, int offset, int count)
        {
            ReadCount++;
            Array.Fill(buffer, 1, offset, count);
            return count;
        }
    }
}
