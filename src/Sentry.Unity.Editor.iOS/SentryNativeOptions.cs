using System.IO;

namespace Sentry.Unity.Editor.iOS
{
    internal interface ISentryNativeOptions
    {
        void CreateOptionsFile(SentryOptions options, string path);
    }

    internal class SentryNativeOptions : ISentryNativeOptions
    {
        public void CreateOptionsFile(SentryOptions options, string path)
        {
            // TODO: options validity checks?

            var nativeOptions = GenerateOptions(options);
            File.WriteAllText(path, nativeOptions);
        }

        internal string GenerateOptions(SentryOptions options)
        {
            var nativeOptions = $@"#import <Foundation/Foundation.h>

// IMPORTANT: Changes to this file will be lost!
// This file is generated during the Xcode project creation.

// TODO: make pretty with link to docs

static NSDictionary* getSentryOptions()
{{
    NSDictionary* options = @{{
        @""dsn"" : @""{options.Dsn}"",
        @""debug"" : @{options.Debug.ToObjCString()},
        @""diagnosticLevel"" : @{options.DiagnosticLevel.ToNativeDiagnosticLevel()},
        @""maxBreadcrumbs"": @{options.MaxBreadcrumbs},
        @""maxCacheItems"": @{options.MaxCacheItems},
        @""enableAutoSessionTracking"": @NO,
        @""send-default-pii"" : @{options.SendDefaultPii.ToObjCString()}
    }};

    return options;
}}";

            return nativeOptions;
        }
    }

    internal static class NativeOptionsUtils
    {
        internal static string ToObjCString(this bool b) => b ? "YES" : "NO";

        // Native Diagnostic Level:
        // None = 0
        // Debug = 1
        // Info = 2
        // Warning = 3
        // Error = 4
        // Fatal = 5
        internal static int ToNativeDiagnosticLevel(this SentryLevel diagnosticLevel) => (int)diagnosticLevel + 1;
    }
}
