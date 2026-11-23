using AuSoCom.Application;
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
    public void DuplicateCreatesASeparateSceneWithCopiesOfRoutingAndMembers()
    {
        var service = new SceneApplicationService();
        var inputId = Guid.NewGuid();
        var outputId = Guid.NewGuid();
        var source = new Scene
        {
            ShortName = "Speech",
            FadeIn = TimeSpan.FromSeconds(2),
            InputIds = [inputId],
            OutputIds = [outputId]
        };
        source.SetRouting(inputId, outputId, 75);
        source.SetInputVolume(inputId, 60);
        source.SetOutputVolume(outputId, 80);
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
        copy.IsInputMuted(inputId).Should().BeTrue();
        copy.IsOutputMuted(outputId).Should().BeTrue();
        copy.Routing.Should().NotBeSameAs(source.Routing);
        copy.InputVolumes.Should().NotBeSameAs(source.InputVolumes);
        copy.OutputVolumes.Should().NotBeSameAs(source.OutputVolumes);
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

        service.RemoveElement(scene, inputId);

        scene.InputIds.Should().BeEmpty();
        scene.Routing.Should().BeEmpty();
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
}
