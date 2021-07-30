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
            var optionsTemplate = $@"// This file is generated during the Xcode project creation.
// Any changes to it WILL be overwritten.

// TODO: make pretty with docs

static NSDictionary* getSentryOptions()
{{
    NSDictionary* options = @{{
        @""dsn"" : @""{options.Dsn}"",
        @""debug"" : @{options.Debug.ToObjCString()},
        @""diagnosticLevel"" : @((SentryLevel){options.DiagnosticLevel.ToNativeDiagnosticLevel()}),
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
