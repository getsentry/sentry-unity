using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Sentry.Unity.Editor.iOS
{
    internal interface INativeMain
    {
        public void AddSentry(string pathToMain);
    }

    internal class NativeMain : INativeMain
    {
        private const string Include = @"#include <Sentry/Sentry.h>
#include ""SentryOptions.m""
";
        private const string Init = @"
        [SentrySDK startWithOptions:getSentryOptions()];
";

        public void AddSentry(string pathToMain)
        {
            if (!DoesMainExist(pathToMain))
            {
                return;
            }

            var main = File.ReadAllText(pathToMain);
            if (ContainsSentry(main))
            {
                return;
            }

            var sentryMain = AddSentryToMain(main);
            if (sentryMain is null)
            {
                return;
            }

            File.WriteAllText(pathToMain, sentryMain);
        }

        internal bool DoesMainExist(string pathToMain)
        {
            if (!File.Exists(pathToMain))
            {
                Debug.LogWarning($"Could not find '{pathToMain}'.");
                return false;
            }

            return true;
        }

        internal bool ContainsSentry(string main)
        {
            if (main.Contains(Include))
            {
                Debug.Log("Sentry already added to 'main.mm'.");
                return true;
            }

            return false;
        }

        internal string? AddSentryToMain(string main)
        {
            main = main.Insert(0, Include);

            var initRegex = new Regex(@"int main\(int argc, char\* argv\[\]\)\s+{\s+@autoreleasepool\s+{");
            var match = initRegex.Match(main);
            if (match.Success)
            {
                return main.Insert(match.Index + match.Length, Init);
            }

            Debug.LogWarning("Failed to add Sentry to main.");
            return null;
        }
    }
}
