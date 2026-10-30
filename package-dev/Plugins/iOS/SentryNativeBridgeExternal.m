#import <Foundation/Foundation.h>
#include <objc/message.h>
#include <objc/runtime.h>

/*******************************************************************************/
/* The bridge for `NativeInitializationType.External`: the app that embeds the */
/* game links its own sentry-cocoa, e.g. Unity as a Library. The export ships  */
/* no headers, so its core classes are only known at runtime. Every call is    */
/* checked with respondsToSelector: first, so a missing selector is skipped    */
/* instead of throwing.                                                        */
/*******************************************************************************/

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

static Class SentrySDKClass;
static Class SentryBreadcrumbClass;

// SentryLastRunStatus.didCrash
static const NSInteger SentryLastRunStatusDidCrash = 2;

// The selectors are getters, which don't return owned objects.
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Warc-performSelector-leaks"
static id SentryCall(id target, SEL selector)
{
    return [target respondsToSelector:selector] ? [target performSelector:selector] : nil;
}

static id SentryCallWith(id target, SEL selector, id argument)
{
    return [target respondsToSelector:selector] ? [target performSelector:selector withObject:argument] : nil;
}

#pragma clang diagnostic pop

// performSelector: is only for methods that return an object, so setters go through a typed objc_msgSend.
static void SentryCallVoid(id target, SEL selector, id argument)
{
    if ([target respondsToSelector:selector]) {
        ((void (*)(id, SEL, id))objc_msgSend)(target, selector, argument);
    }
}

static void SentryCallVoid2(id target, SEL selector, id argument1, id argument2)
{
    if ([target respondsToSelector:selector]) {
        ((void (*)(id, SEL, id, id))objc_msgSend)(target, selector, argument1, argument2);
    }
}

static id SentryGetValue(id object, NSString *key)
{
    return [object respondsToSelector:NSSelectorFromString(key)] ? [object valueForKey:key] : nil;
}

static void SentrySetValue(id object, NSString *key, id value)
{
    NSString *setter = [NSString stringWithFormat:@"set%@%@:", [[key substringToIndex:1] uppercaseString],
                                 [key substringFromIndex:1]];
    if ([object respondsToSelector:NSSelectorFromString(setter)]) {
        [object setValue:value forKey:key];
    }
}

// Returns (bool): 0 on failure, 1 on success
// WARNING: you may only call other Sentry* functions AFTER calling this AND only if it returned "1"
int SentryNativeBridgeLoadLibrary()
{
    if (loadStatus == -1) {
        loadStatus = 0; // init to "error"
        SentrySDKClass = objc_getClass("SentrySDKInternal");
        SentryBreadcrumbClass = objc_getClass("SentryBreadcrumb");
        if (SentrySDKClass && SentryBreadcrumbClass) {
            loadStatus = 1;
        } else {
            NSLog(@"Sentry (bridge): Couldn't find the app's Sentry SDK");
        }
    }
    return loadStatus;
}

int SentryNativeBridgeIsEnabled()
{
    if (SentryNativeBridgeLoadLibrary() != 1 || ![SentrySDKClass respondsToSelector:@selector(isEnabled)]) {
        return 0;
    }
    return ((BOOL (*)(id, SEL))objc_msgSend)(SentrySDKClass, @selector(isEnabled)) ? 1 : 0;
}

// The app starts, names and closes its SDK and owns its user, environment, and trace.
const void *SentryNativeBridgeOptionsNew() { return NULL; }

void SentryNativeBridgeOptionsSetString(const void *options, const char *name, const char *value) { }

void SentryNativeBridgeOptionsSetInt(const void *options, const char *name, int32_t value) { }

void SentryNativeBridgeOptionsSetDouble(const void *options, const char *name, double value) { }

void SentryNativeBridgeOptionsAddFailedRequestStatusCodeRange(const void *options, int32_t min, int32_t max) { }

int SentryNativeBridgeStartWithOptions(const void *options) { return 0; }

void SentryNativeBridgeSetSdkName() { }

void SentryNativeBridgeClose() { }

void SentryNativeBridgeSetEnvironment(const char *environment) { }

void SentryNativeBridgeSetUser(
    const char *email, const char *userId, const char *ipAddress, const char *username) { }

void SentryNativeBridgeUnsetUser() { }

void SentryNativeBridgeSetTrace(const char *traceId, const char *spanId) { }

static void SentryConfigureScope(void (^callback)(id))
{
    SentryCallVoid(SentrySDKClass, @selector(configureScope:), callback);
}

int SentryNativeBridgeCrashedLastRun()
{
    if (![SentrySDKClass respondsToSelector:@selector(lastRunStatus)]) {
        return -1;
    }
    NSInteger lastRunStatus = ((NSInteger (*)(id, SEL))objc_msgSend)(SentrySDKClass, @selector(lastRunStatus));
    return lastRunStatus == SentryLastRunStatusDidCrash ? 1 : 0;
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
                SentrySetValue(breadcrumb, @"timestamp", date);
            }
        }

        if (messageString != nil) {
            SentrySetValue(breadcrumb, @"message", messageString);
        }

        if (typeString != nil) {
            SentrySetValue(breadcrumb, @"type", typeString);
        }

        SentrySetValue(breadcrumb, @"category", categoryString);
        SentrySetValue(breadcrumb, @"level", [NSNumber numberWithInt:level]);

        if (dataCount > 0 && dataKeys != NULL && dataValues != NULL) {
            NSMutableDictionary *data = [NSMutableDictionary dictionaryWithCapacity:dataCount];
            for (int i = 0; i < dataCount; i++) {
                NSString *key = _NSStringOrNil(dataKeys[i]);
                NSString *value = _NSStringOrNil(dataValues[i]);
                if (key != nil && value != nil) {
                    data[key] = value;
                }
            }
            SentrySetValue(breadcrumb, @"data", data);
        }

        SentryCallVoid(scope, @selector(addBreadcrumb:), breadcrumb);
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
        SentryCallVoid2(scope, @selector(setExtraValue:forKey:), valueString, keyString);
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
        SentryCallVoid2(scope, @selector(setTagValue:forKey:), valueString, keyString);
    });
}

void SentryNativeBridgeUnsetTag(const char *key)
{
    if (key == NULL) {
        return;
    }

    NSString *keyString = [NSString stringWithUTF8String:key];

    SentryConfigureScope(^(id scope) {
        SentryCallVoid(scope, @selector(removeTagForKey:), keyString);
    });
}

// Create a null terminated C string on the heap as expected by marshalling.
// See Tips for iOS in https://docs.unity3d.com/Manual/PluginsForIOS.html
static char *SentryCopyString(NSString *value)
{
    if (![value isKindOfClass:[NSString class]]) {
        return NULL;
    }

    const char *nsStringUtf8 = [value UTF8String];
    size_t len = strlen(nsStringUtf8) + 1;
    char *cString = (char *)malloc(len);
    memcpy(cString, nsStringUtf8, len);
    return cString;
}

static id SentryAppOptions(void)
{
    id hub = SentryCall(SentrySDKClass, @selector(currentHub));
    id client = SentryCall(hub, @selector(getClient));
    return SentryGetValue(client, @"options");
}

// Read the ID the way the app's SDK does, from its cache directory.
char *SentryNativeBridgeGetInstallationId()
{
    NSString *cacheDirectoryPath = SentryGetValue(SentryAppOptions(), @"cacheDirectoryPath");
    if (cacheDirectoryPath == nil) {
        return NULL;
    }
    Class installation = NSClassFromString(@"Sentry.SentryInstallation");
    return SentryCopyString(SentryCallWith(installation, @selector(idWithCacheDirectoryPath:), cacheDirectoryPath));
}

// The connection the app's SDK reports in its device context.
void SentryNativeBridgeGetConnection(char **type, char **effectiveType)
{
    id container = SentryCall(NSClassFromString(@"Sentry.SentryDependencyContainer"), @selector(sharedInstance));
    NSDictionary *extraContext = SentryCall(SentryCall(container, @selector(extraContextProvider)), @selector(getExtraContext));
    NSDictionary *device = [extraContext isKindOfClass:[NSDictionary class]] ? extraContext[@"device"] : nil;
    *type = SentryCopyString(device[@"connection_type"]);
    *effectiveType = SentryCopyString(device[@"connection_effective_type"]);
}

// A string option of the app's SDK, e.g. `releaseName`, `environment`, or `dist`.
char *SentryNativeBridgeGetOption(const char *name)
{
    if (name == NULL) {
        return NULL;
    }
    return SentryCopyString(SentryGetValue(SentryAppOptions(), [NSString stringWithUTF8String:name]));
}

// The `sentry-trace` header of the app's SDK: its active span, otherwise its propagation context. The
// propagation context has no sampling decision, so its sampled flag is left out (deferred sampling).
char *SentryNativeBridgeGetTraceHeader()
{
    __block NSString *traceHeader = nil;
    SentryConfigureScope(^(id scope) {
        id span = SentryCall(scope, @selector(span));
        if (span != nil) {
            traceHeader = SentryCall(SentryCall(span, @selector(toTraceHeader)), @selector(value));
            return;
        }
        NSString *value = SentryCall(SentryCall(scope, @selector(propagationContextTraceHeader)), @selector(value));
        NSArray *parts = [value componentsSeparatedByString:@"-"];
        if (parts.count >= 2) {
            traceHeader = [NSString stringWithFormat:@"%@-%@", parts[0], parts[1]];
        }
    });
    return SentryCopyString(traceHeader);
}

// The `baggage` header of the app's SDK's active span, if any.
char *SentryNativeBridgeGetBaggageHeader()
{
    __block NSString *baggageHeader = nil;
    SentryConfigureScope(^(id scope) {
        baggageHeader = SentryCall(SentryCall(scope, @selector(span)), @selector(baggageHttpHeader));
    });
    return SentryCopyString(baggageHeader);
}

// The user on the app's SDK's scope, serialized to JSON.
char *SentryNativeBridgeGetUser()
{
    __block NSString *userJson = nil;
    SentryConfigureScope(^(id scope) {
        NSDictionary *serialized = SentryCall(SentryCall(scope, @selector(userObject)), @selector(serialize));
        if (serialized == nil || ![NSJSONSerialization isValidJSONObject:serialized]) {
            return;
        }
        NSData *data = [NSJSONSerialization dataWithJSONObject:serialized options:0 error:nil];
        userJson = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
    });
    return SentryCopyString(userJson);
}

void SentryNativeBridgeWriteScope( // clang-format off
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
        SentryCallVoid2(scope, @selector(setContextValue:forKey:), gpu, @"gpu");

        NSMutableDictionary *unity = [[NSMutableDictionary alloc] init];
        unity[@"editor_version"] = _NSStringOrNil(EditorVersion);
        unity[@"install_mode"] = _NSStringOrNil(UnityInstallMode);
        unity[@"target_frame_rate"] = _NSStringOrNil(UnityTargetFrameRate);
        unity[@"copy_texture_support"] = _NSStringOrNil(UnityCopyTextureSupport);
        unity[@"rendering_threading_mode"] = _NSStringOrNil(UnityRenderingThreadingMode);
        SentryCallVoid2(scope, @selector(setContextValue:forKey:), unity, @"unity");
    });
}
