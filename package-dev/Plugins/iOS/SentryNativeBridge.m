#import <MetricKit/MetricKit.h>
#import <SentryObjC/SentryObjC.h>
#import <UIKit/UIKit.h>

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

int SentryNativeBridgeIsEnabled() { return [SentryObjCSDK isEnabled] ? 1 : 0; }

const void *SentryNativeBridgeOptionsNew()
{
    NSMutableDictionary *dictOptions = [[NSMutableDictionary alloc] init];
    dictOptions[@"sdk"] = @ { @"name" : @"sentry.cocoa.unity" };
    dictOptions[@"enableAutoSessionTracking"] = @NO;
    dictOptions[@"enableAppHangTracking"] = @NO;
    // Screenshots for C# events come from the Unity SDK. With `attachScreenshot`, sentry-cocoa attaches one
    // only to native crashes: the crash-time screenshot doesn't call this block.
    dictOptions[@"beforeCaptureScreenshot"] = ^BOOL(id event) { return NO; };
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

void SentryNativeBridgeOptionsSetReplay(
    const void *options, double sessionSampleRate, double errorSampleRate, int32_t quality)
{
    NSMutableDictionary *dictOptions = (__bridge NSMutableDictionary *)options;
    dictOptions[@"sessionReplay"] = @{
        @"sessionSampleRate" : @(sessionSampleRate),
        @"errorSampleRate" : @(errorSampleRate),
        @"quality" : @(quality),
    };
}

void SentryNativeBridgeOptionsAddFailedRequestStatusCodeRange(const void *options, int32_t min, int32_t max)
{
    NSMutableDictionary *dictOptions = (__bridge NSMutableDictionary *)options;
    NSMutableArray *ranges = dictOptions[@"failedRequestStatusCodes"];
    if (!ranges) {
        ranges = [[NSMutableArray alloc] init];
        dictOptions[@"failedRequestStatusCodes"] = ranges;
    }
    [ranges addObject:[[SentryObjCHttpStatusCodeRange alloc] initWithMin:min max:max]];
}

int SentryNativeBridgeStartWithOptions(const void *options)
{
    NSMutableDictionary *dictOptions = (__bridge_transfer NSMutableDictionary *)options;
    NSError *error = nil;

    SentryObjCOptions *sentryOptions = [[SentryObjCSDK internal] optionsFromDictionary:dictOptions error:&error];
    if (error != nil)
    {
        return 0;
    }

    [SentryObjCSDK startWithOptions:sentryOptions];
    return 1;
}

void SentryNativeBridgeSetSdkName()
{
    [SentryObjCSDK internal].sdk.name = @"sentry.cocoa.unity";
}

int SentryNativeBridgeCrashedLastRun()
{
    return SentryObjCSDK.lastRunStatus == SentryObjCLastRunStatusDidCrash ? 1 : 0;
}

void SentryNativeBridgeClose() { [SentryObjCSDK close]; }

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

    [SentryObjCSDK configureScope:^(SentryObjCScope *scope) {
        SentryObjCBreadcrumb *breadcrumb = [[SentryObjCBreadcrumb alloc]
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

        if (dataCount > 0 && dataKeys != NULL && dataValues != NULL) {
            NSMutableDictionary *data = [NSMutableDictionary dictionaryWithCapacity:dataCount];
            for (int i = 0; i < dataCount; i++) {
                NSString *key = _NSStringOrNil(dataKeys[i]);
                NSString *value = _NSStringOrNil(dataValues[i]);
                if (key != nil && value != nil) {
                    data[key] = value;
                }
            }
            breadcrumb.data = data;
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

    [SentryObjCSDK configureScope:^(SentryObjCScope *scope) {
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

    [SentryObjCSDK configureScope:^(SentryObjCScope *scope) {
        [scope setTagValue:valueString forKey:keyString];
    }];
}

void SentryNativeBridgeSetEnvironment(const char *environment)
{
    NSString *environmentString = _NSStringOrNil(environment);

    [SentryObjCSDK configureScope:^(SentryObjCScope *scope) {
        [scope setEnvironment:environmentString];
    }];
}

void SentryNativeBridgeUnsetTag(const char *key)
{
    if (key == NULL) {
        return;
    }

    NSString *keyString = [NSString stringWithUTF8String:key];

    [SentryObjCSDK configureScope:^(SentryObjCScope *scope) {
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
    
    [SentryObjCSDK configureScope:^(SentryObjCScope *scope) {
        SentryObjCUser *user = [[SentryObjCUser alloc] init];

        user.email = emailString;
        user.userId = userIdString;
        user.ipAddress = ipAddressString;
        user.username = usernameString;

        [scope setUser:user];
    }];
}

void SentryNativeBridgeUnsetUser()
{
    [SentryObjCSDK configureScope:^(SentryObjCScope *scope) { [scope setUser:nil]; }];
}

char *SentryNativeBridgeGetInstallationId()
{
    // Create a null terminated C string on the heap as expected by marshalling.
    // See Tips for iOS in https://docs.unity3d.com/Manual/PluginsForIOS.html
    const char *nsStringUtf8 = [[SentryObjCSDK internal].sdk.installationID UTF8String];
    size_t len = strlen(nsStringUtf8) + 1;
    char *cString = (char *)malloc(len);
    memcpy(cString, nsStringUtf8, len);
    return cString;
}

static char *_Nullable _CStringCopyOrNull(id _Nullable value)
{
    return [value isKindOfClass:[NSString class]] ? strdup([value UTF8String]) : NULL;
}

void SentryNativeBridgeGetConnection(
    char *_Nullable *_Nonnull type, char *_Nullable *_Nonnull effectiveType)
{
    NSDictionary *device = [SentryObjCSDK internal].sdk.extraContext[@"device"];
    *type = _CStringCopyOrNull(device[@"connection_type"]);
    *effectiveType = _CStringCopyOrNull(device[@"connection_effective_type"]);
}

// Only the app's own SDK provides these, see SentryNativeBridgeExternal.m.
char *_Nullable SentryNativeBridgeGetOption(const char *name) { return NULL; }

char *_Nullable SentryNativeBridgeGetTraceHeader() { return NULL; }

char *_Nullable SentryNativeBridgeGetBaggageHeader() { return NULL; }

char *_Nullable SentryNativeBridgeGetUser() { return NULL; }

void SentryNativeBridgeSetTrace(const char *traceId, const char *spanId)
{
    if (traceId == NULL || spanId == NULL) {
        return;
    }

    NSString *traceIdStr = [NSString stringWithUTF8String:traceId];
    NSString *spanIdStr = [NSString stringWithUTF8String:spanId];

    SentryObjCId *sentryTraceId = [[SentryObjCId alloc] initWithUUIDString:traceIdStr];
    SentryObjCSpanId *sentrySpanId = [[SentryObjCSpanId alloc] initWithValue:spanIdStr];

    if (sentryTraceId && sentrySpanId) {
        [[SentryObjCSDK internal] setTrace:sentryTraceId spanId:sentrySpanId];
    }
}

// Unity capture of session replay: sentry-cocoa asks for each frame on the main thread, and Unity captures it at the
// end of a frame and delivers it. sentry-cocoa takes no further frame until a request completes, so every request
// completes exactly once: with the frame Unity delivers, or with the replay's last frame (or a blank one before the
// first, or when the window's size changed) when Unity cancels it, a new request replaces it, or the replay ends.
@interface SentryUnityReplayFrameProvider : NSObject <SentryObjCReplayScreenshotProvider>
@end

@implementation SentryUnityReplayFrameProvider {
    NSObject *_lock;
    void (^_Nullable _pending)(UIImage *);
    int32_t _pendingId;
    BOOL _taken;
    CGSize _pendingSize;
    int32_t _nextId;
    // The current replay's last delivered frame, and a blank frame of the last size asked for.
    UIImage *_Nullable _lastFrame;
    UIImage *_Nullable _blankFrame;
}

- (instancetype)init
{
    if (self = [super init]) {
        _lock = [[NSObject alloc] init];
    }
    return self;
}

- (void)imageForView:(UIView *)view onComplete:(void (^)(UIImage *))onComplete
{
    void (^replaced)(UIImage *) = nil;
    UIImage *fallback = nil;
    CGSize size = view.bounds.size;
    @synchronized(_lock) {
        replaced = _pending;
        if (replaced) {
            fallback = [self fallbackFrameOfSize:_pendingSize];
        }
        _nextId = _nextId == INT32_MAX ? 1 : _nextId + 1;
        _pending = onComplete;
        _pendingId = _nextId;
        _taken = NO;
        _pendingSize = size;
    }
    if (replaced) {
        replaced(fallback);
    }
}

// The request Unity hasn't taken yet: its id, and the frame's size in points. 0 when there's none.
- (int32_t)takeRequestWithWidth:(int32_t *)width height:(int32_t *)height
{
    @synchronized(_lock) {
        if (!_pending || _taken) {
            return 0;
        }
        _taken = YES;
        *width = (int32_t)lround(_pendingSize.width);
        *height = (int32_t)lround(_pendingSize.height);
        return _pendingId;
    }
}

- (void)deliverRequest:(int32_t)requestId image:(UIImage *)image
{
    void (^completion)(UIImage *) = nil;
    @synchronized(_lock) {
        // A frame for a request that was cancelled may belong to a replay that has ended since.
        if (_pending && _pendingId == requestId) {
            _lastFrame = image;
            completion = _pending;
            _pending = nil;
        }
    }
    if (completion) {
        completion(image);
    }
}

// Completes the pending request with the last frame. With a request id, only that request.
- (void)cancelRequest:(int32_t)requestId
{
    void (^completion)(UIImage *) = nil;
    UIImage *fallback = nil;
    @synchronized(_lock) {
        if (_pending && (requestId == 0 || _pendingId == requestId)) {
            completion = _pending;
            _pending = nil;
            fallback = [self fallbackFrameOfSize:_pendingSize];
        }
    }
    if (completion) {
        completion(fallback);
    }
}

// Completes a pending request and forgets the replay's last frame, so the next replay never shows it. Called when a
// replay ends and before one starts.
- (void)endReplay
{
    [self cancelRequest:0];
    @synchronized(_lock) {
        _lastFrame = nil;
    }
}

static BOOL SentryUnitySameSize(CGSize a, CGSize b)
{
    return lround(a.width) == lround(b.width) && lround(a.height) == lround(b.height);
}

// The last frame when it has the size asked for, otherwise a blank frame of that size. Under the lock.
- (UIImage *)fallbackFrameOfSize:(CGSize)size
{
    CGSize blankSize = CGSizeMake(MAX(lround(size.width), 1), MAX(lround(size.height), 1));
    if (_lastFrame && SentryUnitySameSize(_lastFrame.size, blankSize)) {
        return _lastFrame;
    }
    if (_blankFrame && SentryUnitySameSize(_blankFrame.size, blankSize)) {
        return _blankFrame;
    }
    UIGraphicsImageRendererFormat *format = [UIGraphicsImageRendererFormat preferredFormat];
    format.scale = 1;
    format.opaque = YES;
    UIGraphicsImageRenderer *renderer = [[UIGraphicsImageRenderer alloc] initWithSize:blankSize format:format];
    _blankFrame = [renderer imageWithActions:^(UIGraphicsImageRendererContext *context) {
        [UIColor.blackColor setFill];
        [context fillRect:CGRectMake(0, 0, blankSize.width, blankSize.height)];
    }];
    return _blankFrame;
}

@end

static SentryUnityReplayFrameProvider *_Nullable sentryUnityReplayFrameProvider = nil;

// Session replay. sentry-cocoa runs the replay commands on the main thread.
void SentryNativeBridgeReplayStart()
{
    [sentryUnityReplayFrameProvider endReplay];
    [SentryObjCSDK.internal.replay start];
}

void SentryNativeBridgeReplayStartBuffering()
{
    [sentryUnityReplayFrameProvider endReplay];
    [SentryObjCSDK.internal.replay startBuffering];
}

void SentryNativeBridgeReplayPause() { [SentryObjCSDK.internal.replay pause]; }

void SentryNativeBridgeReplayResume() { [SentryObjCSDK.internal.replay resume]; }

void SentryNativeBridgeReplayStop()
{
    [sentryUnityReplayFrameProvider endReplay];
    [SentryObjCSDK.internal.replay stop];
}

// Captures the replay for an error: a buffered replay is sent and keeps recording as a session. Returns 1 when the
// error belongs to a replay. Capturing runs on the main thread: called from another thread, it waits for it, and
// gives up after half a second, so a busy main thread doesn't hold up the event.
int SentryNativeBridgeReplayCapture()
{
    if ([NSThread isMainThread]) {
        return [SentryObjCSDK.internal.replay capture] ? 1 : 0;
    }

    __block int captured = 0;
    __block BOOL timedOut = NO;
    NSObject *lock = [[NSObject alloc] init];
    dispatch_semaphore_t done = dispatch_semaphore_create(0);
    dispatch_async(dispatch_get_main_queue(), ^{
        @synchronized(lock) {
            if (timedOut) {
                return;
            }
        }
        int result = [SentryObjCSDK.internal.replay capture] ? 1 : 0;
        @synchronized(lock) {
            captured = result;
        }
        dispatch_semaphore_signal(done);
    });

    if (dispatch_semaphore_wait(done, dispatch_time(DISPATCH_TIME_NOW, 500 * NSEC_PER_MSEC)) != 0) {
        @synchronized(lock) {
            timedOut = YES;
            // The capture may have finished between the timeout and taking the lock.
            return captured;
        }
    }
    @synchronized(lock) {
        return captured;
    }
}

// The replay id on the native scope: a replay buffered for errors only gets one once an error captured it.
char *_Nullable SentryNativeBridgeGetReplayId()
{
    __block NSString *replayId = nil;
    [SentryObjCSDK configureScope:^(SentryObjCScope *scope) { replayId = scope.replayId; }];
    return replayId.length > 0 ? strdup(replayId.UTF8String) : NULL;
}

// Lets Unity capture the replay's frames, for every replay started afterwards.
void SentryNativeBridgeReplayUseUnityCapture()
{
    if (!sentryUnityReplayFrameProvider) {
        sentryUnityReplayFrameProvider = [[SentryUnityReplayFrameProvider alloc] init];
    }
    [SentryObjCSDK.internal.replay setScreenshotProvider:sentryUnityReplayFrameProvider];
}

// Returns the id of a frame request Unity hasn't taken yet, with the frame's size in points, or 0 when there's none.
int SentryNativeBridgeReplayTakeFrameRequest(int *width, int *height)
{
    return [sentryUnityReplayFrameProvider takeRequestWithWidth:width height:height];
}

// Completes the request with a frame of top-down RGBA pixels. The pixels are copied before this returns; the image is
// made and handed to sentry-cocoa on a background queue, off Unity's main thread.
void SentryNativeBridgeReplayDeliverFrame(int requestId, const void *rgba, int width, int height)
{
    SentryUnityReplayFrameProvider *provider = sentryUnityReplayFrameProvider;
    if (!provider) {
        return;
    }
    if (!rgba || width <= 0 || height <= 0) {
        [provider cancelRequest:requestId];
        return;
    }

    size_t bytesPerRow = (size_t)width * 4;
    NSData *pixels = [NSData dataWithBytes:rgba length:bytesPerRow * (size_t)height];
    dispatch_async(dispatch_get_global_queue(QOS_CLASS_UTILITY, 0), ^{
        CGDataProviderRef dataProvider = CGDataProviderCreateWithCFData((__bridge CFDataRef)pixels);
        CGColorSpaceRef colorSpace = CGColorSpaceCreateWithName(kCGColorSpaceSRGB);
        // The screen's alpha channel isn't meant to be seen.
        CGImageRef cgImage = CGImageCreate(width, height, 8, 32, bytesPerRow, colorSpace,
            kCGBitmapByteOrderDefault | kCGImageAlphaNoneSkipLast, dataProvider, NULL, false,
            kCGRenderingIntentDefault);
        CGColorSpaceRelease(colorSpace);
        CGDataProviderRelease(dataProvider);
        if (!cgImage) {
            [provider cancelRequest:requestId];
            return;
        }

        UIImage *image = [UIImage imageWithCGImage:cgImage scale:1 orientation:UIImageOrientationUp];
        CGImageRelease(cgImage);
        [provider deliverRequest:requestId image:image];
    });
}

// Completes a pending request with the last frame.
void SentryNativeBridgeReplayCancelFrameRequest() { [sentryUnityReplayFrameProvider cancelRequest:0]; }

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
    [SentryObjCSDK configureScope:^(SentryObjCScope *scope) {
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
        [scope setContextValue:gpu forKey:@"gpu"];

        NSMutableDictionary *unity = [[NSMutableDictionary alloc] init];
        unity[@"editor_version"] = _NSStringOrNil(EditorVersion);
        unity[@"install_mode"] = _NSStringOrNil(UnityInstallMode);
        unity[@"target_frame_rate"] = _NSStringOrNil(UnityTargetFrameRate);
        unity[@"copy_texture_support"] = _NSStringOrNil(UnityCopyTextureSupport);
        unity[@"rendering_threading_mode"] = _NSStringOrNil(UnityRenderingThreadingMode);
        [scope setContextValue:unity forKey:@"unity"];
    }];
}

NS_ASSUME_NONNULL_END
