#import <Sentry/Sentry.h>
#import <MetricKit/MetricKit.h>
#import <Sentry/Sentry-Swift.h>
#import <Sentry/PrivateSentrySDKOnly.h>
#import <Sentry/SentryOptionsInternal.h>

NS_ASSUME_NONNULL_BEGIN

static NSDateFormatter *_Nullable sentry_cachedISO8601Formatter(void) {
    static NSDateFormatter *formatter = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        formatter = [[NSDateFormatter alloc] init];
        formatter.dateFormat = @"yyyy-MM-dd'T'HH:mm:ss'Z'";
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

// macOS only
// On iOS, the SDK is linked statically so we don't need to dlopen() it.
int SentryNativeBridgeLoadLibrary() { return 1; }

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

    SentryOptions *sentryOptions = [SentryOptionsInternal initWithDict:dictOptions didFailWithError:&error];
    if (error != nil)
    {
        return 0;
    }

    [SentrySDK startWithOptions:sentryOptions];
    return 1;
}

void SentryNativeBridgeSetSdkName()
{
    [PrivateSentrySDKOnly performSelector:@selector(setSdkName:) withObject:@"sentry.cocoa.unity"];
}

int SentryNativeBridgeCrashedLastRun() { return [SentrySDK crashedLastRun] ? 1 : 0; }

void SentryNativeBridgeClose() { [SentrySDK close]; }

void SentryNativeBridgeAddBreadcrumb(
    const char *timestamp, const char *message, const char *type, const char *category, int level)
{
    if (timestamp == NULL && message == NULL && type == NULL && category == NULL) {
        return;
    }

    NSString *timestampString = _NSStringOrNil(timestamp);
    NSString *messageString = _NSStringOrNil(message);
    NSString *typeString = _NSStringOrNil(type);
    NSString *categoryString = _NSStringOrNil(category) ?: @"default"; // Category cannot be nil

    [SentrySDK configureScope:^(SentryScope *scope) {
        SentryBreadcrumb *breadcrumb = [[SentryBreadcrumb alloc]
            initWithLevel:level
                 category:categoryString];

        if (timestampString != nil && timestampString.length > 0) {
            NSDate *date = [sentry_cachedISO8601Formatter() dateFromString:timestampString];
            if (date != nil) {
                breadcrumb.timestamp = date;
            }
        }

        if (messageString != nil) {
            breadcrumb.message = messageString;
        }

        if (typeString != nil) {
            breadcrumb.type = typeString;
        }

        [scope addBreadcrumb:breadcrumb];
    }];
}

void SentryNativeBridgeSetExtra(const char *key, const char *value)
{
    if (key == NULL) {
        return;
    }

    NSString *keyString = [NSString stringWithUTF8String:key];
    NSString *valueString = _NSStringOrNil(value);

    [SentrySDK configureScope:^(SentryScope *scope) {
        [scope setExtraValue:valueString forKey:keyString];
    }];
}

void SentryNativeBridgeSetTag(const char *key, const char *value)
{
    if (key == NULL) {
        return;
    }

    NSString *keyString = [NSString stringWithUTF8String:key];
    NSString *valueString = _NSStringOrNil(value);

    [SentrySDK configureScope:^(SentryScope *scope) {
        [scope setTagValue:valueString forKey:keyString];
    }];
}

void SentryNativeBridgeUnsetTag(const char *key)
{
    if (key == NULL) {
        return;
    }

    NSString *keyString = [NSString stringWithUTF8String:key];

    [SentrySDK configureScope:^(SentryScope *scope) {
        [scope removeTagForKey:keyString];
    }];
}

void SentryNativeBridgeSetUser(
    const char *email, const char *userId, const char *ipAddress, const char *username)
{
    NSString *emailString = _NSStringOrNil(email);
    NSString *userIdString = _NSStringOrNil(userId);
    NSString *ipAddressString = _NSStringOrNil(ipAddress);
    NSString *usernameString = _NSStringOrNil(username);
    
    [SentrySDK configureScope:^(SentryScope *scope) {
        SentryUser *user = [[SentryUser alloc] init];

        user.email = emailString;
        user.userId = userIdString;
        user.ipAddress = ipAddressString;
        user.username = usernameString;

        [scope setUser:user];
    }];
}

void SentryNativeBridgeUnsetUser()
{
    [SentrySDK configureScope:^(SentryScope *scope) { [scope setUser:nil]; }];
}

char *SentryNativeBridgeGetInstallationId()
{
    // Create a null terminated C string on the heap as expected by marshalling.
    // See Tips for iOS in https://docs.unity3d.com/Manual/PluginsForIOS.html
    const char *nsStringUtf8 = [[PrivateSentrySDKOnly installationID] UTF8String];
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

    NSString *traceIdStr = [NSString stringWithUTF8String:traceId];
    NSString *spanIdStr = [NSString stringWithUTF8String:spanId];

    // This is a workaround to deal with SentryId living inside the Swift header
    Class sentryIdClass = NSClassFromString(@"_TtC6Sentry8SentryId");
    Class sentrySpanIdClass = NSClassFromString(@"SentrySpanId");

    if (sentryIdClass && sentrySpanIdClass) {
        id sentryTraceId = [[sentryIdClass alloc] initWithUUIDString:traceIdStr];
        id sentrySpanId = [[sentrySpanIdClass alloc] initWithValue:spanIdStr];

        if (sentryTraceId && sentrySpanId) {
            [PrivateSentrySDKOnly setTrace:sentryTraceId spanId:sentrySpanId];
        }
    }
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
    [SentrySDK configureScope:^(SentryScope *scope) {
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
    }];
}

NS_ASSUME_NONNULL_END