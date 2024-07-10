using System;
using System.IO;
using System.Text.RegularExpressions;
using Sentry.Extensibility;

namespace Sentry.Unity.Editor.iOS;

internal static class NativeMain
{
    public const string Include = @"#include <Sentry/Sentry.h>
#include ""SentryOptions.m""
";
    private const string Init = @"
        SentryOptions* options = getSentryOptions();
        if(options != nil)
        {
            [SentrySDK startWithOptions:options];
        }
";

    public static void AddSentry(string pathToMain, IDiagnosticLogger? logger)
    {
        if (!File.Exists(pathToMain))
        {
            throw new FileNotFoundException("Could not find main.", pathToMain);
        }

        var main = File.ReadAllText(pathToMain);
        if (ContainsSentry(main, logger))
        {
            return;
        }

        var sentryMain = AddSentryToMain(main);
        File.WriteAllText(pathToMain, sentryMain);
    }

    internal static bool ContainsSentry(string main, IDiagnosticLogger? logger)
    {
        if (main.Contains(Include))
        {
            logger?.LogInfo("'main.mm' already contains Sentry.");
            return true;
        }

        return false;
    }

    internal static string AddSentryToMain(string main)
    {
        main = main.Insert(0, Include);

        var initRegex = new Regex(@"int main\(int argc, char\* argv\[\]\)\s+{\s+@autoreleasepool\s+{");
        var match = initRegex.Match(main);
        if (match.Success)
        {
            return main.Insert(match.Index + match.Length, Init);
        }

        throw new ArgumentException($"Failed to add Sentry to main.\n{main}", nameof(main));
    }
}