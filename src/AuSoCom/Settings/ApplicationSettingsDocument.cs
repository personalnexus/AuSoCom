namespace AuSoCom.Settings;

public sealed class ApplicationSettingsDocument
{
    public const int MaxRecentFiles = 14;

    public List<string> RecentFiles { get; set; } = [];

    public void AddRecentFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        RecentFiles.RemoveAll(recentFile =>
            string.Equals(recentFile, fullPath, StringComparison.OrdinalIgnoreCase));
        RecentFiles.Insert(0, fullPath);
        if (RecentFiles.Count > MaxRecentFiles)
            RecentFiles.RemoveRange(MaxRecentFiles, RecentFiles.Count - MaxRecentFiles);
    }

    internal void NormalizeRecentFiles()
    {
        if (RecentFiles is null)
            throw new InvalidDataException("Application settings contain a null recent-files list.");

        var uniqueFiles = new List<string>();
        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var recentFile in RecentFiles)
        {
            if (string.IsNullOrWhiteSpace(recentFile))
                throw new InvalidDataException("Application settings contain an empty recent-file path.");

            var fullPath = Path.GetFullPath(recentFile);
            if (seenFiles.Add(fullPath))
                uniqueFiles.Add(fullPath);
            if (uniqueFiles.Count == MaxRecentFiles)
                break;
        }

        RecentFiles = uniqueFiles;
    }
}
