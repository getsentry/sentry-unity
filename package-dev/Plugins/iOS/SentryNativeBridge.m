#import <Foundation/Foundation.h>
#include <objc/message.h>
#include <objc/runtime.h>

static NSDateFormatter *_Nullable sentry_cachedISO8601Formatter(void) {
    static NSDateFormatter *formatter = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        formatter = [[NSDateFormatter alloc] init];
        [formatter setDateFormat:@"yyyy-MM-dd'T'HH:mm:ss'Z'"];
        formatter.timeZone = [NSTimeZone timeZoneWithAbbreviation:@"UTC"];
        formatter.locale = [NSLocale localeWithLocaleIdentifier:@"en_US_POSIX"];
    });
    return formatter;
}

static inline NSString *_NSStringOrNil(const char *value)
{
    return value ? [NSString stringWithUTF8String:value] : nil;
}

static inline NSNumber *_NSNumberOrNil(int32_t value)
{
    return value == 0 ? nil : @(value);
}

static inline NSNumber *_NSBoolOrNil(int8_t value)
{
    if (value == 0) {
        return @NO;
    }
    if (value == 1) {
        return @YES;
    }
    return nil;
}

static int loadStatus = -1; // unitialized

// The SDK the game bundles, through the SentryObjC hybrid API. Missing when the app brings its own SDK.
static Class SentryObjCSDK;
static Class SentryObjCId;
static Class SentryObjCSpanId;
static Class SentryObjCHttpStatusCodeRange;

// Scope sync. Either the SentryObjC classes, or the core classes of the app's own SDK (e.g. Unity as a
// Library). Both take the same selectors and keys.
static Class SentrySDKClass;
static Class SentryBreadcrumbClass;
static Class SentryUserClass;

typedef NS_ENUM(NSInteger, SentryObjCLastRunStatus) {
    SentryObjCLastRunStatusUnknown = 0,
    SentryObjCLastRunStatusDidNotCrash,
    SentryObjCLastRunStatusDidCrash,
};

// [SentryObjCSDK internal] is the hybrid-SDK entry point used for options parsing, SDK metadata,
// and trace propagation.
static id SentryInternalApi(void) { return [SentryObjCSDK performSelector:@selector(internal)]; }

// Returns (bool): 0 on failure, 1 on success
// WARNING: you may only call other Sentry* functions AFTER calling this AND only if it returned "1"
int SentryNativeBridgeLoadLibrary()
{
    if (loadStatus == -1) {
        loadStatus = 0; // init to "error"
        do {
            // The classes are looked up at runtime: the SDK is either linked into UnityFramework, or part of the
            // app that embeds the game.
            SentryObjCSDK = objc_getClass("SentryObjCSDK");
            if (SentryObjCSDK) {
                SentryObjCId = objc_getClass("SentryObjCId");
                SentryObjCSpanId = objc_getClass("SentryObjCSpanId");
                SentryObjCHttpStatusCodeRange = objc_getClass("SentryObjCHttpStatusCodeRange");
                SentrySDKClass = SentryObjCSDK;
                SentryBreadcrumbClass = objc_getClass("SentryObjCBreadcrumb");
                SentryUserClass = objc_getClass("SentryObjCUser");
            } else {
                SentrySDKClass = objc_getClass("SentrySDKInternal");
                SentryBreadcrumbClass = objc_getClass("SentryBreadcrumb");
                SentryUserClass = objc_getClass("SentryUser");
            }

            if (!SentrySDKClass || !SentryBreadcrumbClass || !SentryUserClass) {
                NSLog(@"Sentry (bridge): Couldn't find the Sentry SDK");
                break;
            }

            loadStatus = 1;
        } while (false);
    }
    return loadStatus;
}

int SentryNativeBridgeIsEnabled()
{
    if (SentryNativeBridgeLoadLibrary() != 1) {
        return 0;
    }
    return ((BOOL (*)(id, SEL))objc_msgSend)(SentrySDKClass, @selector(isEnabled)) ? 1 : 0;
}

const void *SentryNativeBridgeOptionsNew()
{
    NSMutableDictionary *dictOptions = [[NSMutableDictionary alloc] init];
    dictOptions[@"sdk"] = @ { @"name" : @"sentry.cocoa.unity" };
    dictOptions[@"enableAutoSessionTracking"] = @NO;
    dictOptions[@"enableAppHangTracking"] = @NO;
    return CFBridgingRetain(dictOptions);
}

void SentryNativeBridgeOptionsSetString(const void *options, const char *name, const char *value)
{
    NSMutableDictionary *dictOptions = (__bridge NSMutableDictionary *)options;
    dictOptions[[NSString stringWithUTF8String:name]] = [NSString stringWithUTF8String:value];
}

void SentryNativeBridgeOptionsSetInt(const void *options, const char *name, int32_t value)
{
    NSMutableDictionary *dictOptions = (__bridge NSMutableDictionary *)options;
    dictOptions[[NSString stringWithUTF8String:name]] = [NSNumber numberWithInt:value];
}

void SentryNativeBridgeOptionsSetDouble(const void *options, const char *name, double value)
{
    NSMutableDictionary *dictOptions = (__bridge NSMutableDictionary *)options;
    dictOptions[[NSString stringWithUTF8String:name]] = [NSNumber numberWithDouble:value];
}

void SentryNativeBridgeOptionsAddFailedRequestStatusCodeRange(const void *options, int32_t min, int32_t max)
{
    NSMutableDictionary *dictOptions = (__bridge NSMutableDictionary *)options;
    NSMutableArray *ranges = dictOptions[@"failedRequestStatusCodes"];
    if (!ranges) {
        ranges = [[NSMutableArray alloc] init];
        dictOptions[@"failedRequestStatusCodes"] = ranges;
    }
    id instance = [SentryObjCHttpStatusCodeRange alloc];
    // initWithMin:max: takes NSInteger args - use objc_msgSend directly
    id range = ((id (*)(id, SEL, NSInteger, NSInteger))objc_msgSend)(
        instance, @selector(initWithMin:max:), (NSInteger)min, (NSInteger)max);
    if (range) {
        [ranges addObject:range];
    }
}

int SentryNativeBridgeStartWithOptions(const void *options)
{
    NSMutableDictionary *dictOptions = (__bridge_transfer NSMutableDictionary *)options;
    NSError *error = nil;

    if (!SentryObjCSDK) {
        return 0;
    }

    id (*optionsFromDictionary)(id, SEL, NSDictionary *, NSError * __autoreleasing *) =
        (void *)objc_msgSend;
    id sentryOptions = optionsFromDictionary(SentryInternalApi(), @selector(optionsFromDictionary:error:),
        dictOptions, &error);

    if (error != nil)
    {
        return 0;
    }

    [SentryObjCSDK performSelector:@selector(startWithOptions:) withObject:sentryOptions];
    return 1;
}

void SentryConfigureScope(void (^callback)(id))
{
    // setValue:forKey: may throw if the property is not found; same for performSelector.
    // Even though this shouldn't happen, better not take the chance of letting an unhandled
    // exception while setting error info - it would just crash the app immediately.
    @try {
        [SentrySDKClass performSelector:@selector(configureScope:) withObject:callback];
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to configure scope: %@", exception.reason);
    }
}

/*******************************************************************************/
/* The classes are only known at runtime. Mainly:                              */
/*   - call: [class performSelector:@selector(arg1:arg2:)                      */
/*                  withObject:arg1Value withObject:arg2Value];                */
/*   - use `id` as variable types                                              */
/*   - use [obj setValue:value forKey:@"prop"] instead of `obj.prop = value`   */
/*******************************************************************************/

void SentryNativeBridgeSetSdkName()
{
    id sdkApi = [SentryInternalApi() performSelector:@selector(sdk)];
    [sdkApi performSelector:@selector(setName:) withObject:@"sentry.cocoa.unity"];
}

int SentryNativeBridgeCrashedLastRun()
{
    @try {
        NSInteger lastRunStatus =
            ((NSInteger (*)(id, SEL))objc_msgSend)(SentrySDKClass, @selector(lastRunStatus));
        return lastRunStatus == SentryObjCLastRunStatusDidCrash ? 1 : 0;
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to get lastRunStatus: %@", exception.reason);
    }
    return -1;
}

void SentryNativeBridgeClose()
{
    @try {
        [SentryObjCSDK performSelector:@selector(close)];
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to close: %@", exception.reason);
    }
}

void SentryNativeBridgeAddBreadcrumb(const char *timestamp, const char *message, const char *type,
    const char *category, int level, const char **dataKeys, const char **dataValues, int dataCount)
{
    if (timestamp == NULL && message == NULL && type == NULL && category == NULL) {
        return;
    }

    NSString *timestampString = _NSStringOrNil(timestamp);
    NSString *messageString = _NSStringOrNil(message);
    NSString *typeString = _NSStringOrNil(type);
    NSString *categoryString = _NSStringOrNil(category) ?: @"default"; // Category cannot be nil

    SentryConfigureScope(^(id scope) {
        id breadcrumb = [[SentryBreadcrumbClass alloc] init];

        if (timestampString != nil && timestampString.length > 0) {
            NSDate *date = [sentry_cachedISO8601Formatter() dateFromString:timestampString];
            if (date != nil) {
                [breadcrumb setValue:date forKey:@"timestamp"];
            }
        }

        if (messageString != nil) {
            [breadcrumb setValue:messageString forKey:@"message"];
        }

        if (typeString != nil) {
            [breadcrumb setValue:typeString forKey:@"type"];
        }

        if (categoryString != nil) {
            [breadcrumb setValue:categoryString forKey:@"category"];
        }

        [breadcrumb setValue:[NSNumber numberWithInt:level] forKey:@"level"];

        if (dataCount > 0 && dataKeys != NULL && dataValues != NULL) {
            NSMutableDictionary *data = [NSMutableDictionary dictionaryWithCapacity:dataCount];
            for (int i = 0; i < dataCount; i++) {
                NSString *key = _NSStringOrNil(dataKeys[i]);
                NSString *value = _NSStringOrNil(dataValues[i]);
                if (key != nil && value != nil) {
                    data[key] = value;
                }
            }
            [breadcrumb setValue:data forKey:@"data"];
        }

        [scope performSelector:@selector(addBreadcrumb:) withObject:breadcrumb];
    });
}

void SentryNativeBridgeSetExtra(const char *key, const char *value)
{
    if (key == NULL) {
        return;
    }

    NSString *keyString = [NSString stringWithUTF8String:key];
    NSString *valueString = _NSStringOrNil(value);

    SentryConfigureScope(^(id scope) {
        [scope performSelector:@selector(setExtraValue:forKey:)
                        withObject:valueString
                        withObject:keyString];
    });
}

void SentryNativeBridgeSetTag(const char *key, const char *value)
{
    if (key == NULL) {
        return;
    }

    NSString *keyString = [NSString stringWithUTF8String:key];
    NSString *valueString = _NSStringOrNil(value);

    SentryConfigureScope(^(id scope) {
        [scope performSelector:@selector(setTagValue:forKey:)
                        withObject:valueString
                        withObject:keyString];
    });
}

void SentryNativeBridgeSetEnvironment(const char *environment)
{
    NSString *environmentString = _NSStringOrNil(environment);

    SentryConfigureScope(^(id scope) {
        [scope performSelector:@selector(setEnvironment:) withObject:environmentString];
    });
}

void SentryNativeBridgeUnsetTag(const char *key)
{
    if (key == NULL) {
        return;
    }

    NSString *keyString = [NSString stringWithUTF8String:key];

    SentryConfigureScope(^(id scope) {
        [scope performSelector:@selector(removeTagForKey:) withObject:keyString];
    });
}

void SentryNativeBridgeSetUser(
    const char *email, const char *userId, const char *ipAddress, const char *username)
{
    NSString *emailString = _NSStringOrNil(email);
    NSString *userIdString = _NSStringOrNil(userId);
    NSString *ipAddressString = _NSStringOrNil(ipAddress);
    NSString *usernameString = _NSStringOrNil(username);

    SentryConfigureScope(^(id scope) {
        id user = [[SentryUserClass alloc] init];

        [user setValue:emailString forKey:@"email"];
        [user setValue:userIdString forKey:@"userId"];
        [user setValue:ipAddressString forKey:@"ipAddress"];
        [user setValue:usernameString forKey:@"username"];

        [scope performSelector:@selector(setUser:) withObject:user];
    });
}

void SentryNativeBridgeUnsetUser()
{
    SentryConfigureScope(
        ^(id scope) { [scope performSelector:@selector(setUser:) withObject:nil]; });
}

// Create a null terminated C string on the heap as expected by marshalling.
// See Tips for iOS in https://docs.unity3d.com/Manual/PluginsForIOS.html
static char *SentryCopyString(NSString *value)
{
    if (value == nil) {
        return NULL;
    }

    const char *nsStringUtf8 = [value UTF8String];
    size_t len = strlen(nsStringUtf8) + 1;
    char *cString = (char *)malloc(len);
    memcpy(cString, nsStringUtf8, len);
    return cString;
}

// The options of the app's own SDK. Through `SentrySDKInternal`, which every distribution has.
static id SentryAppOptions(void)
{
    id hub = [objc_getClass("SentrySDKInternal") performSelector:@selector(currentHub)];
    id client = [hub performSelector:@selector(getClient)];
    return [client valueForKey:@"options"];
}

static NSString *SentryInstallationId(void)
{
    if (SentryObjCSDK) {
        id sdkApi = [SentryInternalApi() performSelector:@selector(sdk)];
        return [sdkApi performSelector:@selector(installationID)];
    }

    // The app's own SDK: read the ID the way it does, from its cache directory.
    @try {
        NSString *cacheDirectoryPath = [SentryAppOptions() valueForKey:@"cacheDirectoryPath"];
        Class installation = NSClassFromString(@"Sentry.SentryInstallation");
        if (cacheDirectoryPath == nil
            || ![installation respondsToSelector:@selector(idWithCacheDirectoryPath:)]) {
            return nil;
        }
        return [installation performSelector:@selector(idWithCacheDirectoryPath:)
                                  withObject:cacheDirectoryPath];
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to get the installation ID: %@", exception.reason);
        return nil;
    }
}

char *SentryNativeBridgeGetInstallationId() { return SentryCopyString(SentryInstallationId()); }

// A string option of the app's own SDK, e.g. `releaseName`, `environment`, or `dist`.
char *SentryNativeBridgeGetOption(const char *name)
{
    if (name == NULL) {
        return NULL;
    }

    @try {
        id value = [SentryAppOptions() valueForKey:[NSString stringWithUTF8String:name]];
        return [value isKindOfClass:[NSString class]] ? SentryCopyString(value) : NULL;
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to get option '%s': %@", name, exception.reason);
        return NULL;
    }
}

// The `sentry-trace` header of the app's own SDK: its active span, otherwise its propagation context.
char *SentryNativeBridgeGetTraceHeader()
{
    __block NSString *traceHeader = nil;
    SentryConfigureScope(^(id scope) {
        id header = nil;
        id span = [scope respondsToSelector:@selector(span)] ? [scope performSelector:@selector(span)] : nil;
        if (span != nil && [span respondsToSelector:@selector(toTraceHeader)]) {
            header = [span performSelector:@selector(toTraceHeader)];
        } else if ([scope respondsToSelector:@selector(propagationContextTraceHeader)]) {
            header = [scope performSelector:@selector(propagationContextTraceHeader)];
        }
        if ([header respondsToSelector:@selector(value)]) {
            traceHeader = [header performSelector:@selector(value)];
        }
    });
    return SentryCopyString(traceHeader);
}

void SentryNativeBridgeSetTrace(const char *traceId, const char *spanId)
{
    if (traceId == NULL || spanId == NULL) {
        return;
    }

    id sentryTraceId = ((id (*)(id, SEL, NSString *))objc_msgSend)(
        [SentryObjCId alloc], @selector(initWithUUIDString:),
        [NSString stringWithUTF8String:traceId]);

    id sentrySpanId = ((id (*)(id, SEL, NSString *))objc_msgSend)(
        [SentryObjCSpanId alloc], @selector(initWithValue:),
        [NSString stringWithUTF8String:spanId]);

    [SentryInternalApi()
        performSelector:@selector(setTrace:spanId:)
        withObject:sentryTraceId
        withObject:sentrySpanId];
}

void SentryNativeBridgeWriteScope( // clang-format off
    // // const char *AppStartTime,
    // const char *AppBuildType,
    // // const char *OperatingSystemRawDescription,
    // int DeviceProcessorCount,
    // const char *DeviceCpuDescription,
    // const char *DeviceTimezone,
    // int8_t DeviceSupportsVibration,
    // const char *DeviceName,
    // int8_t DeviceSimulator,
    // const char *DeviceDeviceUniqueIdentifier,
    // const char *DeviceDeviceType,
    // // const char *DeviceModel,
    // // long DeviceMemorySize,
    int32_t GpuId,
    const char *GpuName,
    const char *GpuVendorName,
    int32_t GpuMemorySize,
    const char *GpuNpotSupport,
    const char *GpuVersion,
    const char *GpuApiType,
    int32_t GpuMaxTextureSize,
    int8_t GpuSupportsDrawCallInstancing,
    int8_t GpuSupportsRayTracing,
    int8_t GpuSupportsComputeShaders,
    int8_t GpuSupportsGeometryShaders,
    const char *GpuVendorId,
    int8_t GpuMultiThreadedRendering,
    const char *GpuGraphicsShaderLevel,
    const char *EditorVersion,
    const char *UnityInstallMode,
    const char *UnityTargetFrameRate,
    const char *UnityCopyTextureSupport,
    const char *UnityRenderingThreadingMode
) // clang-format on
{
    // Note: we're using a NSMutableDictionary because it will skip fields with nil values.
    SentryConfigureScope(^(id scope) {
        NSMutableDictionary *gpu = [[NSMutableDictionary alloc] init];
        gpu[@"id"] = _NSNumberOrNil(GpuId);
        gpu[@"name"] = _NSStringOrNil(GpuName);
        gpu[@"vendor_name"] = _NSStringOrNil(GpuVendorName);
        gpu[@"memory_size"] = _NSNumberOrNil(GpuMemorySize);
        gpu[@"npot_support"] = _NSStringOrNil(GpuNpotSupport);
        gpu[@"version"] = _NSStringOrNil(GpuVersion);
        gpu[@"api_type"] = _NSStringOrNil(GpuApiType);
        gpu[@"max_texture_size"] = _NSNumberOrNil(GpuMaxTextureSize);
        gpu[@"supports_draw_call_instancing"] = _NSBoolOrNil(GpuSupportsDrawCallInstancing);
        gpu[@"supports_ray_tracing"] = _NSBoolOrNil(GpuSupportsRayTracing);
        gpu[@"supports_compute_shaders"] = _NSBoolOrNil(GpuSupportsComputeShaders);
        gpu[@"supports_geometry_shaders"] = _NSBoolOrNil(GpuSupportsGeometryShaders);
        gpu[@"vendor_id"] = _NSStringOrNil(GpuVendorId);
        gpu[@"multi_threaded_rendering"] = _NSBoolOrNil(GpuMultiThreadedRendering);
        gpu[@"graphics_shader_level"] = _NSStringOrNil(GpuGraphicsShaderLevel);
        [scope performSelector:@selector(setContextValue:forKey:) withObject:gpu withObject:@"gpu"];

        NSMutableDictionary *unity = [[NSMutableDictionary alloc] init];
        unity[@"editor_version"] = _NSStringOrNil(EditorVersion);
        unity[@"install_mode"] = _NSStringOrNil(UnityInstallMode);
        unity[@"target_frame_rate"] = _NSStringOrNil(UnityTargetFrameRate);
        unity[@"copy_texture_support"] = _NSStringOrNil(UnityCopyTextureSupport);
        unity[@"rendering_threading_mode"] = _NSStringOrNil(UnityRenderingThreadingMode);
        [scope performSelector:@selector(setContextValue:forKey:)
                    withObject:unity
                    withObject:@"unity"];
    });
}
