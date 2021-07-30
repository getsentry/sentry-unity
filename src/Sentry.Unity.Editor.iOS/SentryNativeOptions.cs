namespace Sentry.Unity.Editor.iOS
{
    public static class SentryNativeOptions
    {
        private static string ToObjCString(this bool b) => b ? "YES" : "NO";

        // Native Diagnostic Level:
        // None = 0
        // Debug = 1
        // Info = 2
        // Warning = 3
        // Error = 4
        // Fatal = 5
        private static int ToNativeDiagnosticLevel(this SentryLevel diagnosticLevel) => (int)diagnosticLevel + 1;

        public static string GenerateOptions(SentryOptions options)
        {
            var optionsTemplate = $@"#import <Foundation/Foundation.h>

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

            return optionsTemplate;
        }
    }
}
