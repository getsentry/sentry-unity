#import <Foundation/Foundation.h>
#include <dlfcn.h>

static NSDateFormatter *_Nullable cachedISO8601Formatter(void) {
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

static int loadStatus = -1; // unitialized

static Class SentrySDK;
static Class SentryScope;
static Class SentryBreadcrumb;
static Class SentryUser;
static Class SentryOptions;
static Class SentryOptionsInternal;
static Class SentryId;
static Class SentrySpanId;
static Class PrivateSentrySDKOnly;

#define LOAD_CLASS_OR_BREAK(name)                                                                  \
    name = (__bridge Class)dlsym(dylib, "OBJC_CLASS_$_" #name);                                    \
    if (!name) {                                                                                   \
        NSLog(@"Sentry (bridge): Couldn't load class '" #name "' from the dynamic library");       \
        break;                                                                                     \
    }

#define LOAD_SWIFT_CLASS_OR_BREAK(name, mangled_name)                                              \
    name = (__bridge Class)dlsym(dylib, "OBJC_CLASS_$_" #mangled_name);                            \
    if (!name) {                                                                                   \
        NSLog(@"Sentry (bridge): Couldn't load class '" #name "' from the dynamic library");       \
        break;                                                                                     \
    }

// Returns (bool): 0 on failure, 1 on success
// WARNING: you may only call other Sentry* functions AFTER calling this AND only if it returned "1"
int SentryNativeBridgeLoadLibrary()
{
    if (loadStatus == -1) {
        loadStatus = 0; // init to "error"
        do {
            // The default path from the executable to the dylib within a .app
            void *dylib = dlopen("@executable_path/../PlugIns/Sentry.dylib", RTLD_LAZY);
            if (!dylib) {
                // Fallback path for the dedicated server setup
                dylib = dlopen("@executable_path/PlugIns/Sentry.dylib", RTLD_LAZY);
                if (!dylib) {
                    NSLog(@"Sentry (bridge): Couldn't load Sentry.dylib - dlopen() failed");
                    break;
                }
            }

            LOAD_SWIFT_CLASS_OR_BREAK(SentrySDK, _TtC6Sentry9SentrySDK)
            LOAD_CLASS_OR_BREAK(SentryScope)
            LOAD_CLASS_OR_BREAK(SentryBreadcrumb)
            LOAD_CLASS_OR_BREAK(SentryUser)
            LOAD_CLASS_OR_BREAK(SentryOptions)
            LOAD_CLASS_OR_BREAK(SentryOptionsInternal)
            LOAD_SWIFT_CLASS_OR_BREAK(SentryId, _TtC6Sentry8SentryId)
            LOAD_CLASS_OR_BREAK(SentrySpanId)
            LOAD_CLASS_OR_BREAK(PrivateSentrySDKOnly)

            // everything above passed - mark as successfully loaded
            loadStatus = 1;
        } while (false);
    }
    return loadStatus;
}

int SentryNativeBridgeIsEnabled() { return [SentrySDK isEnabled] ? 1 : 0; }

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

int SentryNativeBridgeStartWithOptions(const void *options)
{
    NSMutableDictionary *dictOptions = (__bridge_transfer NSMutableDictionary *)options;
    NSError *error = nil;

    id sentryOptions = [SentryOptionsInternal
        performSelector:@selector(initWithDict:didFailWithError:)
        withObject:dictOptions withObject:&error];

    if (error != nil)
    {
        return 0;
    }

    [SentrySDK performSelector:@selector(startWithOptions:) withObject:sentryOptions];
    return 1;
}

void SentryConfigureScope(void (^callback)(id))
{
    // setValue:forKey: may throw if the property is not found; same for performSelector.
    // Even though this shouldn't happen, better not take the chance of letting an unhandled
    // exception while setting error info - it would just crash the app immediately.
    @try {
        [SentrySDK performSelector:@selector(configureScope:) withObject:callback];
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to configure scope: %@", exception.reason);
    }
}

/*******************************************************************************/
/* The remaining code is a copy of iOS/SentryNativeBridge.m with changes to    */
/* make it work with dynamically loaded classes. Mainly:                       */
/*   - call: [class performSelector:@selector(arg1:arg2:)                      */
/*                  withObject:arg1Value withObject:arg2Value];                */
/*     or xCode warns of class/instance method not found                       */
/*   - use `id` as variable types                                              */
/*   - use [obj setValue:value forKey:@"prop"] instead of `obj.prop = value`   */
/*******************************************************************************/

void SentryNativeBridgeSetSdkName()
{
    [PrivateSentrySDKOnly performSelector:@selector(setSdkName:) withObject:@"sentry.cocoa.unity"];
}

int SentryNativeBridgeCrashedLastRun()
{
    @try {
        return [SentrySDK performSelector:@selector(crashedLastRun)] ? 1 : 0;
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to get crashedLastRun: %@", exception.reason);
    }
    return -1;
}

void SentryNativeBridgeClose()
{
    @try {
        [SentrySDK performSelector:@selector(close)];
    } @catch (NSException *exception) {
        NSLog(@"Sentry (bridge): failed to close: %@", exception.reason);
    }
}

void SentryNativeBridgeAddBreadcrumb(
    const char *timestamp, const char *message, const char *type, const char *category, int level)
{
    if (timestamp == NULL && message == NULL && type == NULL && category == NULL) {
        return;
    }

    NSString *timestampString = _NSStringOrNil(timestamp):
    NSString *messageString = _NSStringOrNil(message):
    NSString *typeString = _NSStringOrNil(type);
    NSString *categoryString = _NSStringOrNil(category) ?: @"default"; // Category cannot be nil

    SentryConfigureScope(^(id scope) {
        id breadcrumb = [[SentryBreadcrumb alloc] init];

        if (timestampString != nil && timestampString.length > 0) {
            NSDate *date = [cachedISO8601Formatter() dateFromString:timestampString];
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
        if (valueString != nil) {
            [scope performSelector:@selector(setExtraValue:forKey:)
                        withObject:valueString
                        withObject:keyString];
        } else {
            [scope performSelector:@selector(removeExtraForKey:)
                        withObject:keyString];
        }
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
        if (valueString != nil) {
            [scope performSelector:@selector(setTagValue:forKey:)
                        withObject:valueString
                        withObject:keyString];
        } else {
            [scope performSelector:@selector(removeTagForKey:)
                        withObject:keyString];
        }
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
    if (email == NULL && userId == NULL && ipAddress == NULL && username == NULL) {
        return;
    }

    NSString *emailString _NSStringOrNil(email);
    NSString *userIdString = _NSStringOrNil(userId);
    NSString *ipAddressString = _NSStringOrNil(ipAddress);
    NSString *usernameString = _NSStringOrNil(username);

    SentryConfigureScope(^(id scope) {
        id user = [[SentryUser alloc] init];

        if (emailString != nil) {
            [user setValue:emailString forKey:@"email"];
        }

        if (userIdString != nil) {
            [user setValue:userIdString forKey:@"userId"];
        }

        if (ipAddressString != nil) {
            [user setValue:ipAddressString forKey:@"ipAddress"];
        }

        if (usernameString != nil) {
            [user setValue:usernameString forKey:@"username"];
        }

        [scope performSelector:@selector(setUser:) withObject:user];
    });
}

void SentryNativeBridgeUnsetUser()
{
    SentryConfigureScope(
        ^(id scope) { [scope performSelector:@selector(setUser:) withObject:nil]; });
}

char *SentryNativeBridgeGetInstallationId()
{
    // Create a null terminated C string on the heap as expected by marshalling.
    // See Tips for iOS in https://docs.unity3d.com/Manual/PluginsForIOS.html
    const char *nsStringUtf8 =
        [[PrivateSentrySDKOnly performSelector:@selector(installationID)] UTF8String];
    size_t len = strlen(nsStringUtf8) + 1;
    char *cString = (char *)malloc(len);
    memcpy(cString, nsStringUtf8, len);
    return cString;
}

void SentryNativeBridgeSetTrace(const char *traceId, const char *spanId)
{
    if (traceId == NULL || spanId == NULL) {
        return;
    }

    id sentryTraceId = [[SentryId alloc] 
        performSelector:@selector(initWithUUIDString:) 
        withObject:[NSString stringWithUTF8String:traceId]];
        
    id sentrySpanId = [[SentrySpanId alloc]
        performSelector:@selector(initWithValue:)
        withObject:[NSString stringWithUTF8String:spanId]];
    
    [PrivateSentrySDKOnly 
        performSelector:@selector(setTrace:spanId:) 
        withObject:sentryTraceId 
        withObject:sentrySpanId];
}

static inline NSString *_NSStringOrNil(const char *value)
{
    return value ? [NSString stringWithUTF8String:value] : nil;
}

static inline NSString *_NSNumberOrNil(int32_t value)
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