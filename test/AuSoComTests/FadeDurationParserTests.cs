using AuSoCom.Application;
using FluentAssertions;

namespace AuSoComTests;

[TestClass]
public sealed class FadeDurationParserTests
{
    [TestMethod]
    [DataRow("250 ms", 250d)]
    [DataRow("1.5 s", 1500d)]
    [DataRow("01:02.500", 62500d)]
    [DataRow("2", 2d)]
    public void ParsesSupportedFormatsAsDurations(string input, double expectedMilliseconds)
    {
        FadeDurationParser.TryParse(input, out var duration).Should().BeTrue();
        duration.Should().Be(TimeSpan.FromMilliseconds(expectedMilliseconds));
    }

    [TestMethod]
    public void EmptyInputClearsTheDuration()
    {
        FadeDurationParser.TryParse(" ", out var duration).Should().BeTrue();
        duration.Should().BeNull();
    }

    [TestMethod]
    [DataRow("-1")]
    [DataRow("not a duration")]
    [DataRow("1:99.000")]
    public void RejectsInvalidDurations(string input)
    {
        FadeDurationParser.TryParse(input, out _).Should().BeFalse();
    }
}
