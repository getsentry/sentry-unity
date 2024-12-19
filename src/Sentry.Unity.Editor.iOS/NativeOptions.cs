using System.Collections.Generic;
using System.IO;

namespace Sentry.Unity.Editor.iOS;

internal static class NativeOptions
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
        @""dsn"" : @""{options.Dsn}"",
        @""debug"" : @{ToObjCString(options.Debug)},
        @""diagnosticLevel"" : @""{ToNativeDiagnosticLevel(options.DiagnosticLevel)}"",
        @""maxBreadcrumbs"": @{options.MaxBreadcrumbs},
        @""maxCacheItems"": @{options.MaxCacheItems},
        @""enableAutoSessionTracking"": @NO,
        @""enableAppHangTracking"": @NO,
        @""enableCaptureFailedRequests"": @{ToObjCString(options.CaptureFailedRequests)},
        @""failedRequestStatusCodes"" : @[{failedRequestStatusCodesArray}],
        @""sendDefaultPii"" : @{ToObjCString(options.SendDefaultPii)},
        @""attachScreenshot"" : @{ToObjCString(options.AttachScreenshot)},
        @""release"" : @""{options.Release}"",
        @""environment"" : @""{options.Environment}"",
        @""enableNetworkBreadcrumbs"" : @NO,
        // TODO temp code, implement this properly
        @""experimental"" : @{{
            @""sessionReplay"" : @{{
                @""sessionSampleRate"" : @1.0,
                @""errorSampleRate"" : @1.0
            }}
        }}
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
