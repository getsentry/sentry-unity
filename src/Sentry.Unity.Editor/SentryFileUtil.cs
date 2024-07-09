using System.IO;

namespace Sentry.Unity.Editor;

public static class SentryFileUtil
{
    internal static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        var directory = new DirectoryInfo(sourceDirectory);
        if (!directory.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {directory.FullName}");
        }

        var subDirectories = directory.GetDirectories();
        Directory.CreateDirectory(destinationDirectory);

        foreach (var file in directory.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDirectory, file.Name);
            file.CopyTo(targetFilePath);
        }

        foreach (var subDirectory in subDirectories)
        {
            var newDestinationDir = Path.Combine(destinationDirectory, subDirectory.Name);
            CopyDirectory(subDirectory.FullName, newDestinationDir);
        }
    }
}
