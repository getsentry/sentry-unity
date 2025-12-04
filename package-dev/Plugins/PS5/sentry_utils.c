#include <stdarg.h>
#include <stdio.h>

/**
 * Wrapper around vsnprintf for Unity C# IL2CPP interop.
 *
 * This function provides a stable ABI entry point for Unity to call
 * vsnprintf functionality across Windows, Linux, and PlayStation platforms.
 *
 * This wrapper is compiled as native C code where va_list is properly
 * understood, then IL2CPP calls this wrapper with generic pointers.
 */
int vsnprintf_sentry(char *buffer, size_t size, const char *format, va_list args)
{
    return vsnprintf(buffer, size, format, args);
}
