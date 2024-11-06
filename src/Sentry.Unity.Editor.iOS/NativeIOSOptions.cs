using System.Collections.Generic;
using System.IO;

namespace Sentry.Unity.Editor.iOS;

internal static class NativeIOSOptions
{
    public static void CreateFile(string path, SentryUnityOptions options) => File.WriteAllText(path, Generate(options));

    internal static string Generate(SentryUnityOptions options)
    {
        var failedRequestStatusCodesArray = GetFailedRequestStatusCodesArray(options.FailedRequestStatusCodes);
        var nativeOptions = $@"#import <Foundation/Foundation.h>
#import <Sentry/SentryOptions+HybridSDKs.h>
#import <Sentry/PrivateSentrySDKOnly.h>

// IMPORTANT: Changes to this file will be lost!
// This file is generated during the Xcode project creation.

// To learn more please take a look at our docs at: https://docs.sentry.io/platforms/unity/native-support/

static SentryOptions* getSentryOptions()
{{
    [PrivateSentrySDKOnly setSdkName:@""sentry.cocoa.unity""];

    NSDictionary* optionsDictionary = @{{
        @""dsn"" : @""{options.Native.Dsn ?? options.Dsn}"",
        @""debug"" : @{ToObjCString(options.Native.Debug ?? options.Debug)},
        @""diagnosticLevel"" : @""{ToNativeDiagnosticLevel(options.Native.DiagnosticLevel ?? options.DiagnosticLevel)}"",
        @""maxBreadcrumbs"": @{options.Native.MaxBreadcrumb ?? options.MaxBreadcrumbs},
        @""maxCacheItems"": @{options.Native.MaxCacheItem ?? options.MaxCacheItems},
        @""enableAutoSessionTracking"": @NO,
        @""enableAppHangTracking"": @NO,
        @""enableCaptureFailedRequests"": @{ToObjCString(options.CaptureFailedRequests)},
        @""failedRequestStatusCodes"" : @[{failedRequestStatusCodesArray}],
        @""sendDefaultPii"" : @{ToObjCString(options.Native.SendDefaultPii ?? options.SendDefaultPii)},
        @""attachScreenshot"" : @{ToObjCString(options.Native.AttachScreenshot ?? options.AttachScreenshot)},
        @""release"" : @""{options.Native.Release ?? options.Release}"",
        @""environment"" : @""{options.Native.Environment ?? options.Environment}"",
        @""enableNetworkBreadcrumbs"" : @NO
    }};

    NSError *error = nil;
    SentryOptions* options = [[SentryOptions alloc] initWithDict:optionsDictionary didFailWithError:&error];
    if (error != nil)
    {{
        NSLog(@""%@"",[error localizedDescription]);
        return nil;
    }}

    {(options.FilterBadGatewayExceptions ? @"options.beforeSend = ^SentryEvent * _Nullable(SentryEvent * _Nonnull event) {
        if ([event.request.url containsString:@""operate-sdk-telemetry.unity3d.com""]) return nil;
        return event;
    };" : "")}

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

    private static string GetFailedRequestStatusCodesArray(IList<HttpStatusCodeRange> httpStatusCodeRanges)
    {
        var codeRanges = string.Empty;
        for (var i = 0; i < httpStatusCodeRanges.Count; i++)
        {
            codeRanges += $"[[SentryHttpStatusCodeRange alloc] initWithMin:{httpStatusCodeRanges[i].Start} max:{httpStatusCodeRanges[i].End}]";
            if (i < httpStatusCodeRanges.Count - 1)
            {
                codeRanges += ", ";
            }
        }

        return codeRanges;
    }
}
