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
            FadeIn = TimeSpan.FromMilliseconds(500),
            FadeOut = TimeSpan.FromSeconds(1),
            AutoStart = true,
            InputIds = [inputId],
            OutputIds = [outputId]
        };
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
            loaded.Scenes[0].FadeIn.Should().Be(TimeSpan.FromMilliseconds(500));
            loaded.Scenes[0].FadeOut.Should().Be(TimeSpan.FromSeconds(1));
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
}
