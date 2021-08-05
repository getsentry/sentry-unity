using System.IO;

namespace Sentry.Unity.Editor.iOS
{
    internal interface INativeOptions
    {
        public void CreateFile(string path, SentryOptions options);
    }

    internal class NativeOptions : INativeOptions
    {
        public void CreateFile(string path, SentryOptions options) => File.WriteAllText(path, Generate(options));

        internal string Generate(SentryOptions options)
        {
            var nativeOptions = $@"#import <Foundation/Foundation.h>

// IMPORTANT: Changes to this file will be lost!
// This file is generated during the Xcode project creation.

// TODO: make pretty with link to docs

static NSDictionary* getSentryOptions()
{{
    NSDictionary* options = @{{
        @""dsn"" : @""{options.Dsn}"",
        @""debug"" : @{ToObjCString(options.Debug)},
        @""diagnosticLevel"" : @{ToNativeDiagnosticLevel(options.DiagnosticLevel)},
        @""maxBreadcrumbs"": @{options.MaxBreadcrumbs},
        @""maxCacheItems"": @{options.MaxCacheItems},
        @""enableAutoSessionTracking"": @NO,
        @""sendDefaultPii"" : @{ToObjCString(options.SendDefaultPii)}
    }};

    return options;
}}";

            return nativeOptions;
        }

        private static string ToObjCString(bool b) => b ? "YES" : "NO";

        // Native Diagnostic Level:
        // None = 0
        // Debug = 1
        // Info = 2
        // Warning = 3
        // Error = 4
        // Fatal = 5
        private static int ToNativeDiagnosticLevel(SentryLevel diagnosticLevel) => (int)diagnosticLevel + 1;
    }
}
