namespace Sentry.Unity.Native;

/// <summary>
/// The library name this build binds its P/Invokes to.
/// </summary>
/// <remarks>
/// Desktop cannot use "sentry": Mono probes the calling assembly's own directory first, where it
/// resolves to the managed `Sentry.dll` on a case-insensitive file system. Android and the consoles
/// get their library from elsewhere and keep the original name.
/// </remarks>
internal static class SentryNativeLibrary
{
#if SENTRY_NATIVE_SWITCH
    internal const string Name = "__Internal";
#elif SENTRY_NATIVE_ANDROID || SENTRY_NATIVE_PLAYSTATION || SENTRY_NATIVE_XBOX
    internal const string Name = "sentry";
#else
    internal const string Name = "sentry-native";
#endif
}
