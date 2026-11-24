using AuSoCom.Application;
using AuSoCom.Audio;
using AuSoCom.Domain;
using FluentAssertions;

namespace AuSoComTests;

[TestClass]
public sealed class SceneTests
{
    [TestMethod]
    public void NewSceneIncludesOnlyDevicesMarkedAutoAdd()
    {
        var service = new SceneApplicationService();
        var autoInput = new Device { Role = DeviceRole.InputStereo, AutoAdd = true };
        var regularInput = new Device { Role = DeviceRole.InputLeft };
        var autoOutput = new Device { Role = DeviceRole.OutputStereo, AutoAdd = true };
        var scene = service.CreateScene("New scene", [autoInput, regularInput, autoOutput]);

        scene.InputIds.Should().Equal(autoInput.Id);
        scene.OutputIds.Should().Equal(autoOutput.Id);
    }

    [TestMethod]
    public void SceneCreatedForFilesIncludesFilesAndAutoAddedOutputsButNotAutoAddedInputs()
    {
        var service = new SceneApplicationService();
        var autoInput = new Device { Role = DeviceRole.InputStereo, AutoAdd = true };
        var autoOutput = new Device { Role = DeviceRole.OutputStereo, AutoAdd = true };
        var firstFile = new MediaFile { ShortName = "First" };
        var secondFile = new MediaFile { ShortName = "Second" };

        var scene = service.CreateSceneForFiles(
            "Files scene",
            [autoInput, autoOutput],
            [firstFile, secondFile]);

        scene.InputIds.Should().Equal(firstFile.Id, secondFile.Id);
        scene.InputIds.Should().NotContain(autoInput.Id);
        scene.OutputIds.Should().Equal(autoOutput.Id);
    }

    [TestMethod]
    public void DuplicateCreatesASeparateSceneWithCopiesOfRoutingAndMembers()
    {
        var service = new SceneApplicationService();
        var inputId = Guid.NewGuid();
        var outputId = Guid.NewGuid();
        var source = new Scene
        {
            ShortName = "Speech",
            InputIds = [inputId],
            OutputIds = [outputId]
        };
        source.SetRouting(inputId, outputId, 75);
        source.SetInputVolume(inputId, 60);
        source.SetOutputVolume(outputId, 80);
        source.SetInputGainDb(inputId, -6);
        source.SetOutputGainDb(outputId, 3);
        source.SetInputDelay(inputId, TimeSpan.FromMilliseconds(250));
        source.SetInputFadeIn(inputId, TimeSpan.FromSeconds(2));
        source.SetInputFadeOut(inputId, TimeSpan.FromSeconds(1));
        source.SetInputMuted(inputId, true);
        source.SetOutputMuted(outputId, true);

        var copy = service.Duplicate(source);

        copy.Id.Should().NotBe(source.Id);
        copy.ShortName.Should().Be("Speech copy");
        copy.InputIds.Should().Equal(inputId);
        copy.OutputIds.Should().Equal(outputId);
        copy.GetRouting(inputId, outputId).Should().Be(75);
        copy.GetInputVolume(inputId).Should().Be(60);
        copy.GetOutputVolume(outputId).Should().Be(80);
        copy.GetInputGainDb(inputId).Should().Be(-6);
        copy.GetOutputGainDb(outputId).Should().Be(3);
        copy.GetInputDelay(inputId).Should().Be(TimeSpan.FromMilliseconds(250));
        copy.GetInputFadeIn(inputId).Should().Be(TimeSpan.FromSeconds(2));
        copy.GetInputFadeOut(inputId).Should().Be(TimeSpan.FromSeconds(1));
        copy.IsInputMuted(inputId).Should().BeTrue();
        copy.IsOutputMuted(outputId).Should().BeTrue();
        copy.Routing.Should().NotBeSameAs(source.Routing);
        copy.InputVolumes.Should().NotBeSameAs(source.InputVolumes);
        copy.OutputVolumes.Should().NotBeSameAs(source.OutputVolumes);
        copy.InputGainsDb.Should().NotBeSameAs(source.InputGainsDb);
        copy.OutputGainsDb.Should().NotBeSameAs(source.OutputGainsDb);
        copy.InputDelays.Should().NotBeSameAs(source.InputDelays);
        copy.InputFadeIns.Should().NotBeSameAs(source.InputFadeIns);
        copy.InputFadeOuts.Should().NotBeSameAs(source.InputFadeOuts);
    }

    [TestMethod]
    public void RemovingAnElementAlsoRemovesItsRouting()
    {
        var service = new SceneApplicationService();
        var inputId = Guid.NewGuid();
        var outputId = Guid.NewGuid();
        var scene = new Scene
        {
            InputIds = [inputId],
            OutputIds = [outputId]
        };
        scene.SetRouting(inputId, outputId, 100);
        scene.SetInputGainDb(inputId, 6);
        scene.SetOutputGainDb(outputId, -3);
        scene.SetInputDelay(inputId, TimeSpan.FromMilliseconds(10));
        scene.SetInputFadeIn(inputId, TimeSpan.FromMilliseconds(20));
        scene.SetInputFadeOut(inputId, TimeSpan.FromMilliseconds(30));

        service.RemoveElement(scene, inputId);

        scene.InputIds.Should().BeEmpty();
        scene.Routing.Should().BeEmpty();
        scene.InputGainsDb.Should().BeEmpty();
        scene.InputDelays.Should().BeEmpty();
        scene.InputFadeIns.Should().BeEmpty();
        scene.InputFadeOuts.Should().BeEmpty();
        scene.OutputGainsDb.Should().ContainKey(outputId.ToString("N"));

        service.RemoveElement(scene, outputId);

        scene.OutputGainsDb.Should().BeEmpty();
    }

    [TestMethod]
    public void RoutingRejectsValuesOutsideThePercentageRange()
    {
        var scene = new Scene();
        var inputId = Guid.NewGuid();
        var outputId = Guid.NewGuid();

        var action = () => scene.SetRouting(inputId, outputId, 101);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void RoutingDefaultsToFullVolumeUntilConfigured()
    {
        var scene = new Scene();
        var inputId = Guid.NewGuid();
        var outputId = Guid.NewGuid();

        scene.GetRouting(inputId, outputId).Should().Be(100);

        scene.SetRouting(inputId, outputId, 0);

        scene.GetRouting(inputId, outputId).Should().Be(0);
    }

    [TestMethod]
    public void VolumeControlsDefaultToFullAndRejectValuesOutsideThePercentageRange()
    {
        var scene = new Scene();
        var inputId = Guid.NewGuid();

        scene.GetInputVolume(inputId).Should().Be(100);
        var action = () => scene.SetInputVolume(inputId, 101);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void GainControlsDefaultToUnityAndRejectValuesOutsideTheDecibelRange()
    {
        var scene = new Scene();
        var inputId = Guid.NewGuid();

        scene.GetInputGainDb(inputId).Should().Be(0);
        scene.SetInputGainDb(inputId, -50);
        scene.SetOutputGainDb(inputId, 50);
        var tooLow = () => scene.SetInputGainDb(inputId, -50.1);
        var tooHigh = () => scene.SetOutputGainDb(inputId, 50.1);

        tooLow.Should().Throw<ArgumentOutOfRangeException>();
        tooHigh.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void InputTimingControlsDefaultToEmptyAndRejectNegativeDurations()
    {
        var scene = new Scene();
        var inputId = Guid.NewGuid();

        scene.GetInputDelay(inputId).Should().BeNull();
        scene.GetInputFadeIn(inputId).Should().BeNull();
        scene.GetInputFadeOut(inputId).Should().BeNull();
        var action = () => scene.SetInputFadeIn(inputId, TimeSpan.FromMilliseconds(-1));

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void RouteGainAppliesInputAndOutputDecibelGain()
    {
        var scene = new Scene();
        var inputId = Guid.NewGuid();
        var outputId = Guid.NewGuid();
        scene.SetInputGainDb(inputId, 6);

        NAudioEngine.GetRouteGain(scene, inputId, outputId)
            .Should().BeApproximately(1.995262f, 0.0001f);
    }
}
