using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Sentry.Unity.Editor.iOS
{
    internal interface IMainModifier
    {
        public void AddSentry(string mainPath);
    }

    internal class MainModifier : IMainModifier
    {
        private const string Include = @"#include <Sentry/Sentry.h>
#include ""SentryOptions.m""
";
        private const string Init = @"
        [SentrySDK startWithOptions:getSentryOptions()];
";

        public void AddSentry(string mainPath)
        {
            if (!DoesMainExist(mainPath))
            {
                return;
            }

            var main = File.ReadAllText(mainPath);
            if (ContainsSentry(main))
            {
                return;
            }

            var sentryMain = AddSentryToMain(main);
            if (sentryMain is null)
            {
                return;
            }

            File.WriteAllText(mainPath, sentryMain);
        }

        internal bool DoesMainExist(string mainPath)
        {
            if (!File.Exists(mainPath))
            {
                Debug.LogWarning($"Could not find '{mainPath}'.");
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
