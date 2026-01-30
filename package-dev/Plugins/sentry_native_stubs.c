/*
 * Sentry Switch Stubs
 *
 * No-op stub implementations for sentry-native and Switch helper functions.
 * These stubs are used when the user has not provided the actual sentry-switch
 * native library, allowing the SDK to compile and run without native crash support.
 *
 * When the real sentry-switch library is provided by the user at:
 *   Assets/Plugins/Sentry/Switch/libsentry.a
 *   Assets/Plugins/Sentry/Switch/SentrySwitchHelpers.cpp
 *
 * This stub file will be automatically disabled by the build preprocessor,
 * and the real library will be linked instead.
 *
 * All functions here are no-ops that return safe default values.
 * The SDK will appear to initialize successfully, but native features
 * (crash reporting, native scope sync) will silently do nothing.
 * Managed Sentry features continue to work normally.
 */

#include <stddef.h>
#include <stdint.h>
#include <stdarg.h>

/* sentry_value_t is an opaque 64-bit union in sentry-native */
typedef union {
    uint64_t _bits;
    double _double;
} sentry_value_t;

/* Null value constant */
static const sentry_value_t SENTRY_VALUE_NULL = {0};

/*
 * =============================================================================
 * sentry-native Core Functions
 * =============================================================================
 */

void* sentry_options_new(void)
{
    /* Return non-null to indicate "success" - value is opaque anyway */
    return (void*)1;
}

int sentry_init(void* options)
{
    /* Return 0 to indicate success */
    return 0;
}

void sentry_close(void)
{
    /* No-op */
}

/*
 * =============================================================================
 * sentry_options_set_* Functions (No-op)
 * =============================================================================
 */

void sentry_options_set_dsn(void* options, const char* dsn)
{
    (void)options;
    (void)dsn;
}

void sentry_options_set_release(void* options, const char* release)
{
    (void)options;
    (void)release;
}

void sentry_options_set_environment(void* options, const char* environment)
{
    (void)options;
    (void)environment;
}

void sentry_options_set_debug(void* options, int debug)
{
    (void)options;
    (void)debug;
}

void sentry_options_set_sample_rate(void* options, double rate)
{
    (void)options;
    (void)rate;
}

void sentry_options_set_database_path(void* options, const char* path)
{
    (void)options;
    (void)path;
}

void sentry_options_set_auto_session_tracking(void* options, int track)
{
    (void)options;
    (void)track;
}

void sentry_options_set_attach_screenshot(void* options, int attach)
{
    (void)options;
    (void)attach;
}

void sentry_options_set_logger(void* options, void* logger, void* userdata)
{
    (void)options;
    (void)logger;
    (void)userdata;
}

/*
 * =============================================================================
 * sentry_value_* Functions
 * =============================================================================
 */

sentry_value_t sentry_value_new_null(void)
{
    return SENTRY_VALUE_NULL;
}

sentry_value_t sentry_value_new_bool(int value)
{
    (void)value;
    return SENTRY_VALUE_NULL;
}

sentry_value_t sentry_value_new_int32(int32_t value)
{
    (void)value;
    return SENTRY_VALUE_NULL;
}

sentry_value_t sentry_value_new_double(double value)
{
    (void)value;
    return SENTRY_VALUE_NULL;
}

sentry_value_t sentry_value_new_string(const char* value)
{
    (void)value;
    return SENTRY_VALUE_NULL;
}

sentry_value_t sentry_value_new_object(void)
{
    return SENTRY_VALUE_NULL;
}

sentry_value_t sentry_value_new_breadcrumb(const char* type, const char* message)
{
    (void)type;
    (void)message;
    return SENTRY_VALUE_NULL;
}

int sentry_value_set_by_key(sentry_value_t value, const char* k, sentry_value_t v)
{
    (void)value;
    (void)k;
    (void)v;
    return 0;
}

int sentry_value_is_null(sentry_value_t value)
{
    (void)value;
    /* Return 1 (true) - all stub values are effectively null */
    return 1;
}

int32_t sentry_value_as_int32(sentry_value_t value)
{
    (void)value;
    return 0;
}

double sentry_value_as_double(sentry_value_t value)
{
    (void)value;
    return 0.0;
}

const char* sentry_value_as_string(sentry_value_t value)
{
    (void)value;
    return NULL;
}

size_t sentry_value_get_length(sentry_value_t value)
{
    (void)value;
    return 0;
}

sentry_value_t sentry_value_get_by_index(sentry_value_t value, size_t index)
{
    (void)value;
    (void)index;
    return SENTRY_VALUE_NULL;
}

sentry_value_t sentry_value_get_by_key(sentry_value_t value, const char* key)
{
    (void)value;
    (void)key;
    return SENTRY_VALUE_NULL;
}

void sentry_value_decref(sentry_value_t value)
{
    (void)value;
}

/*
 * =============================================================================
 * Scope/Context Functions (No-op)
 * =============================================================================
 */

void sentry_set_context(const char* key, sentry_value_t value)
{
    (void)key;
    (void)value;
}

void sentry_add_breadcrumb(sentry_value_t breadcrumb)
{
    (void)breadcrumb;
}

void sentry_set_tag(const char* key, const char* value)
{
    (void)key;
    (void)value;
}

void sentry_remove_tag(const char* key)
{
    (void)key;
}

void sentry_set_user(sentry_value_t user)
{
    (void)user;
}

void sentry_remove_user(void)
{
    /* No-op */
}

void sentry_set_extra(const char* key, sentry_value_t value)
{
    (void)key;
    (void)value;
}

void sentry_remove_extra(const char* key)
{
    (void)key;
}

void sentry_set_trace(const char* trace_id, const char* parent_span_id)
{
    (void)trace_id;
    (void)parent_span_id;
}

/*
 * =============================================================================
 * Crash Detection Functions
 * =============================================================================
 */

int sentry_get_crashed_last_run(void)
{
    /* Return 0 - no crash detected (since we're not tracking) */
    return 0;
}

int sentry_clear_crashed_last_run(void)
{
    return 0;
}

void sentry_reinstall_backend(void)
{
    /* No-op */
}

sentry_value_t sentry_get_modules_list(void)
{
    /* Return null - no modules to report */
    return SENTRY_VALUE_NULL;
}

/*
 * =============================================================================
 * Switch Helper Functions
 * =============================================================================
 */

int sentry_switch_utils_mount(void)
{
    /* Return 1 to indicate success - allows SDK initialization to proceed */
    return 1;
}

const char* sentry_switch_utils_get_cache_path(void)
{
    /* Return a valid-looking path */
    return "sentry:/";
}

int sentry_switch_utils_is_mounted(void)
{
    /* Return 1 - pretend we're mounted */
    return 1;
}

void sentry_switch_utils_unmount(void)
{
    /* No-op */
}

const char* sentry_switch_utils_get_default_user_id(void)
{
    /* Return empty string - no user ID available */
    return "";
}

/*
 * =============================================================================
 * Utility Functions
 * =============================================================================
 */

int vsnprintf_sentry(char* buffer, size_t size, const char* format, va_list args)
{
    (void)format;
    (void)args;

    /* Just null-terminate the buffer and return 0 */
    if (buffer != NULL && size > 0)
    {
        buffer[0] = '\0';
    }
    return 0;
}
