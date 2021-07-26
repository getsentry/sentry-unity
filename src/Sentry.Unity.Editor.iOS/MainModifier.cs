using System.Text.RegularExpressions;

namespace Sentry.Unity.Editor.iOS
{
    public static class MainModifier
    {
        private const string Include = "#include <Sentry/Sentry.h>\n#include \"SentryOptions.m\"\n";
        private const string Init = "\t\t[SentrySDK startWithOptions:GetOptions()];\n\n";

        public static string? GetModifiedMain(string main)
        {
            if(main.Contains(Include))
            {
                // TODO: Handling of "we already modified this main"
                return null;
            }

            var modifiedMain = main.Insert(0, Include);

            var initRegex = new Regex(@"int main\(int argc, char\* argv\[\]\)\n{\n\s+@autoreleasepool\n.\s+{\n");
            var match = initRegex.Match(modifiedMain);
            if (match.Success)
            {
                modifiedMain = modifiedMain.Insert(match.Index + match.Length, Init);
            }

            return modifiedMain;
        }
    }
}
