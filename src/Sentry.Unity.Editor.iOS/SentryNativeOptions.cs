namespace Sentry.Unity.Editor.iOS
{
    public static class SentryNativeOptions
    {
        private static string ToObjCString(this bool b) => b ? "YES" : "NO";

        public static string GenerateOptions(SentryOptions options)
        {
            var optionsTemplate = $@"#import <Foundation/Foundation.h>

// IMPORTANT: DO NOT TOUCH! This file is generated during the Xcode project creation.
// Your changes WILL be overwritten.

// TODO: make pretty with docs maybe?

static NSDictionary* getSentryOptions()
{{
    NSDictionary* options = @{{
        @""dsn"" : @""{options.Dsn}"",
        @""enableAutoSessionTracking"": @{options.AutoSessionTracking.ToObjCString()},
        @""debug"" : @{options.Debug.ToObjCString()}
    }};

    return options;
}}";

            return optionsTemplate;
        }
    }
}
