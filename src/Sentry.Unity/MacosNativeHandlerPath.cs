using System;
using System.IO;

namespace Sentry.Unity;

/// <summary>
/// Finds the <c>sentry-crash</c> daemon the macOS player ships inside <c>SentryCrash.bundle</c>.
/// </summary>
internal static class MacosNativeHandlerPath
{
    internal const string BundleRelativePath = "PlugIns/SentryCrash.bundle/Contents/MacOS/sentry-crash";

    /// <summary>
    /// Returns the first daemon that exists, or <c>null</c> when there is none.
    /// </summary>
    /// <param name="dataPath">The player's <c>Application.dataPath</c>.</param>
    /// <param name="fileExists">Checks whether a file exists.</param>
    /// <param name="expected">The path the daemon is expected at in a player app.</param>
    internal static string? Resolve(string dataPath, Func<string, bool> fileExists, out string expected)
    {
        // A player app: dataPath is `<name>.app/Contents`, and Unity puts plugins in `Contents/PlugIns`.
        expected = Path.Combine(dataPath, BundleRelativePath);
        if (fileExists(expected))
        {
            return expected;
        }

        // A dedicated server: dataPath is `<name>_Data`, and the plugins sit next to the executable.
        var serverRoot = Path.GetDirectoryName(dataPath);
        if (!string.IsNullOrEmpty(serverRoot))
        {
            var server = Path.Combine(serverRoot, BundleRelativePath);
            if (fileExists(server))
            {
                return server;
            }
        }

        return null;
    }
}
