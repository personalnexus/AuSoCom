using AuSoCom.Audio;
using FluentAssertions;

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
}
