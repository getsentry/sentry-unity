/*
 * Sentry Switch Stubs
 *
 * sentry-unity stub version: 2
 *
 * No-op stub implementations for sentry-native and Switch helper functions. They keep a Switch or
 * Switch 2 player linking for anyone who does not have sentry-switch, which is distributed under
 * NDA, so the SDK never breaks a build it cannot complete.
 *
 * THIS FILE IS COPIED INTO THE CONSUMER PROJECT. `SwitchNativeStub` writes it to
 * Assets/Plugins/Sentry/<target>/sentry_native_stubs.c whenever the real libraries are missing, and
 * deletes it again once they appear. Do not edit the copy; edit this original and it propagates on
 * the next domain reload.
 *
 * BUMP THE VERSION ABOVE WHENEVER THIS FILE CHANGES. It is the whole of how the SDK decides that a
 * copy already sitting in someone's project is behind, and a binding added without a stub to answer
 * it is a Switch build that fails to link on a symbol nobody has heard of.
 *
 * The real libraries the copy stands in for:
 *   Assets/Plugins/Sentry/Switch/libsentry.a   + libzstd.a
 *   Assets/Plugins/Sentry/Switch2/libsentry.a  + libzstd.a
 *
 * Every function here returns a safe default. `sentry_init` returns failure, which is what
 * `SentryNativeSwitch` keys off to report that native support is unavailable, so a stubbed build
 * says so instead of silently reporting nothing.
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
    /* Return -1 to indicate failure to let sentry-unity degrade gracefully */
    return -1;
}

int sentry_close(void)
{
    /* Success, matching sentry-native's `int sentry_close(void)` */
    return 0;
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

void sentry_options_set_enable_metrics(void* options, int enable_metrics)
{
    (void)options;
    (void)enable_metrics;
}

void sentry_options_set_enable_logs(void* options, int enable_logs)
{
    (void)options;
    (void)enable_logs;
}

void sentry_options_set_shutdown_timeout(void* options, uint64_t shutdown_timeout)
{
    (void)options;
    (void)shutdown_timeout;
}

void sentry_options_set_enable_app_hang_tracking(void* options, int enabled)
{
    (void)options;
    (void)enabled;
}

void sentry_options_set_app_hang_timeout(void* options, uint64_t timeout)
{
    (void)options;
    (void)timeout;
}

void sentry_options_set_logger(void* options, void* logger, void* userdata)
{
    (void)options;
    (void)logger;
    (void)userdata;
}

void sentry_options_set_logger_enabled_when_crashed(void* options, int enabled)
{
    (void)options;
    (void)enabled;
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

int sentry_value_decref(sentry_value_t value)
{
    /* Refcount reached zero, matching sentry-native's `int sentry_value_decref(sentry_value_t)` */
    (void)value;
    return 0;
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

void sentry_set_environment(const char* environment)
{
    (void)environment;
}

void* sentry_attach_file(const char* path)
{
    (void)path;
    return NULL;
}

void* sentry_attach_bytes(const char* buffer, size_t buffer_length, const char* filename)
{
    (void)buffer;
    (void)buffer_length;
    (void)filename;
    return NULL;
}

void sentry_clear_attachments(void)
{
    /* No-op */
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

int sentry_reinstall_backend(void)
{
    /* Success, matching sentry-native's `int sentry_reinstall_backend(void)` */
    return 0;
}

void sentry_app_hang_heartbeat(void)
{
    /* No-op */
}

void sentry_app_hang_pause(void)
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

int sentry_switch_utils_is_network_available(void)
{
    /* No native SDK to ask - assume usable and let the transport's backoff take over */
    return 1;
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
