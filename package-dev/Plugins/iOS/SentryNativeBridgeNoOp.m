NS_ASSUME_NONNULL_BEGIN

// macOS only
int SentryNativeBridgeLoadLibrary() { return 0; }
void *_Nullable SentryNativeBridgeOptionsNew() { return nil; }
void SentryNativeBridgeOptionsSetString(void *options, const char *name, const char *value) { }
void SentryNativeBridgeOptionsSetInt(void *options, const char *name, int32_t value) { }
void SentryNativeBridgeStartWithOptions(void *options) { }

int SentryNativeBridgeCrashedLastRun() { return 0; }

void SentryNativeBridgeClose() { }

void SentryNativeBridgeAddBreadcrumb(
    const char *timestamp, const char *message, const char *type, const char *category, int level) { }

void SentryNativeBridgeSetExtra(const char *key, const char *value) { }

void SentryNativeBridgeSetTag(const char *key, const char *value) { }

void SentryNativeBridgeUnsetTag(const char *key) { }

void SentryNativeBridgeSetUser(
const char *email, const char *userId, const char *ipAddress, const char *username) { }

void SentryNativeBridgeUnsetUser() { }

char *SentryNativeBridgeGetInstallationId() { return NULL; }

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
{ }

NS_ASSUME_NONNULL_END
