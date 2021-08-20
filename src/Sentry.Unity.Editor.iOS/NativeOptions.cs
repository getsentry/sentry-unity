using System;
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
        @""diagnosticLevel"" : @""{ToNativeDiagnosticLevel(options.DiagnosticLevel)}"",
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

        private static string ToNativeDiagnosticLevel(SentryLevel sentryLevel)
        {
            return sentryLevel switch
            {
                SentryLevel.Debug => "debug",
                SentryLevel.Info => "info",
                SentryLevel.Warning => "warning",
                SentryLevel.Error => "error",
                SentryLevel.Fatal => "fatal",
                _ => "none"
            };
        }
    }
}
