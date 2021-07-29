namespace Sentry.Unity.Editor.iOS
{
    public static class SentryNativeOptions
    {
        private static string ToObjCString(this bool b) => b ? "YES" : "NO";

        private int

        public static string GenerateOptions(SentryOptions options)
        {
            var optionsTemplate = $@"// This file is generated during the Xcode project creation.
// Any changes to it WILL be overwritten.

// TODO: make pretty with docs

static NSDictionary* getSentryOptions()
{{
    NSDictionary* options = @{{
        @""dsn"" : @""{options.Dsn}"",
        @""enableAutoSessionTracking"": @NO,
        @""debug"" : @{options.Debug.ToObjCString()}
        @""send-default-pii"" : @{options.SendDefaultPii.ToObjCString()}
    }};

    return options;
}}";

            return optionsTemplate;
        }
    }
}
