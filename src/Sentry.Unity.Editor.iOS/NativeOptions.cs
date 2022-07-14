using System.IO;

namespace Sentry.Unity.Editor.iOS
{
    internal interface INativeOptions
    {
        public void CreateFile(string path, SentryUnityOptions options);
    }

    internal class NativeOptions : INativeOptions
    {
        public void CreateFile(string path, SentryUnityOptions options) => File.WriteAllText(path, Generate(options));

        internal string Generate(SentryUnityOptions options)
        {
            var nativeOptions = $@"#import <Foundation/Foundation.h>

// IMPORTANT: Changes to this file will be lost!
// This file is generated during the Xcode project creation.

// To learn more please take a look at our docs at: https://docs.sentry.io/platforms/unity/native-support/

static NSDictionary* getSentryOptions()
{{
    NSDictionary* options = @{{
        @""sdk"" : @{{ @""name"": @""sentry.cocoa.unity"" }},
        @""dsn"" : @""{options.Dsn}"",
        @""debug"" : @{ToObjCString(options.Debug)},
        @""diagnosticLevel"" : @""{ToNativeDiagnosticLevel(options.DiagnosticLevel)}"",
        @""maxBreadcrumbs"": @{options.MaxBreadcrumbs},
        @""maxCacheItems"": @{options.MaxCacheItems},
        @""enableAutoSessionTracking"": @NO,
        @""enableAppHangTracking"": @NO,
        @""sendDefaultPii"" : @{ToObjCString(options.SendDefaultPii)},
        @""attachScreenshot"" : @""{options.AttachScreenshot}"",
        @""release"" : @""{options.Release}"",
        @""environment"" : @""{options.Environment}""
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
