#include <stdarg.h>
#include <stdio.h>

/**
 * Wrapper around vsnprintf for Unity C# IL2CPP interop.
 *
 * This function provides a stable ABI entry point for Unity to call
 * vsnprintf functionality on PlayStation platforms. IL2CPP cannot directly
 * call the vsnprintf from the Prospero SDK because:
 * 1. IL2CPP generates a forward declaration with generic pointer types
 * 2. This conflicts with the actual vsnprintf signature in stdio.h
 * 3. va_list requires proper platform-specific handling
 *
 * This wrapper is compiled as native C code where va_list is properly
 * understood, then IL2CPP calls this wrapper with generic pointers.
 */
int vsnprintf_ps(char *buffer, size_t size, const char *format, va_list args)
{
    return vsnprintf(buffer, size, format, args);
}
