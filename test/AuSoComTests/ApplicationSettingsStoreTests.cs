using AuSoCom.Settings;
using FluentAssertions;

namespace AuSoComTests;

[TestClass]
public sealed class ApplicationSettingsStoreTests
{
    [TestMethod]
    public void RecentFilesAreDeduplicatedAndLimitedToFourteenAcrossSaveAndLoad()
    {
        var settingsPath = Path.Combine(Path.GetTempPath(), $"ausocom-{Guid.NewGuid():N}.appsettings.json");
        var recentPaths = Enumerable.Range(0, 16)
            .Select(index => Path.Combine(Path.GetTempPath(), $"project-{index}.json"))
            .ToArray();

        try
        {
            var settings = new ApplicationSettingsDocument();
            foreach (var path in recentPaths)
                settings.AddRecentFile(path);
            var duplicateWithDifferentCasing = recentPaths[14].ToUpperInvariant();
            settings.AddRecentFile(duplicateWithDifferentCasing);

            var store = new ApplicationSettingsStore(settingsPath);
            store.Save(settings);
            var loaded = store.Load();

            loaded.RecentFiles.Should().HaveCount(ApplicationSettingsDocument.MaxRecentFiles);
            loaded.RecentFiles.Should().Equal(new[] { duplicateWithDifferentCasing, recentPaths[15] }
                .Concat(recentPaths.Skip(2).Take(12).Reverse())
                .Select(Path.GetFullPath));
            loaded.RecentFiles.Distinct(StringComparer.OrdinalIgnoreCase).Should().HaveCount(14);
        }
        finally
        {
            if (File.Exists(settingsPath)) File.Delete(settingsPath);
            var temporaryPath = settingsPath + ".tmp";
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
