using AuSoCom.Domain;
using AuSoCom.Settings;
using FluentAssertions;

namespace AuSoComTests;

[TestClass]
public sealed class SettingsStoreTests
{
    [TestMethod]
    public async Task SettingsRoundTripPreservesGuidsAndSceneRouting()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ausocom-{Guid.NewGuid():N}.json");
        var inputId = Guid.NewGuid();
        var outputId = Guid.NewGuid();
        var scene = new Scene
        {
            ShortName = "Main",
            AutoStart = true,
            InputIds = [inputId],
            OutputIds = [outputId]
        };
        scene.SetInputDelay(inputId, TimeSpan.FromMilliseconds(250));
        scene.SetInputFadeIn(inputId, TimeSpan.FromMilliseconds(500));
        scene.SetInputFadeOut(inputId, TimeSpan.FromSeconds(1));
        scene.SetRouting(inputId, outputId, 50);
        var settings = new SettingsDocument();
        var device = new Device
        {
            ShortName = "Main output",
            SystemName = "Main output device",
            Role = DeviceRole.OutputStereo,
            Favorite = true,
            AutoAdd = true
        };
        settings.Devices.Add(device);
        settings.Scenes.Add(scene);
        settings.Show.Add(scene.Id);

        try
        {
            var store = new SettingsStore();
            store.Save(path, settings);
            var loaded = store.Load(path);

            loaded.Scenes.Should().ContainSingle();
            loaded.Scenes[0].Id.Should().Be(scene.Id);
            loaded.Scenes[0].GetRouting(inputId, outputId).Should().Be(50);
            loaded.Scenes[0].GetInputDelay(inputId).Should().Be(TimeSpan.FromMilliseconds(250));
            loaded.Scenes[0].GetInputFadeIn(inputId).Should().Be(TimeSpan.FromMilliseconds(500));
            loaded.Scenes[0].GetInputFadeOut(inputId).Should().Be(TimeSpan.FromSeconds(1));
            loaded.Scenes[0].AutoStart.Should().BeTrue();
            loaded.Devices.Should().ContainSingle();
            loaded.Devices[0].Favorite.Should().BeTrue();
            loaded.Devices[0].AutoAdd.Should().BeTrue();
            loaded.Show.SceneIds.Should().Equal(scene.Id);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            var temporaryPath = path + ".tmp";
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    [TestMethod]
    public void SettingsLoadMigratesLegacySceneTimingToEachInput()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ausocom-{Guid.NewGuid():N}.json");
        var firstInputId = Guid.NewGuid();
        var secondInputId = Guid.NewGuid();
        var json = $$"""
            {
              "Scenes": [
                {
                  "InputIds": ["{{firstInputId}}", "{{secondInputId}}"],
                  "Delay": "00:00:00.2500000",
                  "FadeIn": "00:00:00.5000000",
                  "FadeOut": "00:00:01.0000000"
                }
              ]
            }
            """;

        try
        {
            File.WriteAllText(path, json);

            var scene = new SettingsStore().Load(path).Scenes.Single();

            scene.GetInputDelay(firstInputId).Should().Be(TimeSpan.FromMilliseconds(250));
            scene.GetInputDelay(secondInputId).Should().Be(TimeSpan.FromMilliseconds(250));
            scene.GetInputFadeIn(firstInputId).Should().Be(TimeSpan.FromMilliseconds(500));
            scene.GetInputFadeIn(secondInputId).Should().Be(TimeSpan.FromMilliseconds(500));
            scene.GetInputFadeOut(firstInputId).Should().Be(TimeSpan.FromSeconds(1));
            scene.GetInputFadeOut(secondInputId).Should().Be(TimeSpan.FromSeconds(1));
            scene.LegacyDelay.Should().BeNull();
            scene.LegacyFadeIn.Should().BeNull();
            scene.LegacyFadeOut.Should().BeNull();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            var temporaryPath = path + ".tmp";
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
