namespace Sentry.Unity.Native;

/// <summary>
/// The name this build of the assembly binds its P/Invokes to.
/// </summary>
/// <remarks>
/// Desktop cannot bind to plain "sentry". Mono probes the calling assembly's own directory first,
/// and on a case-insensitive file system that resolves to the managed `Sentry.dll` sitting next to
/// us in `Managed/`. It loads, the C entry point is missing, and native support dies with an
/// `EntryPointNotFoundException`. `BuildPostProcess` copies the library into the player under the
/// renamed variant instead. Android and the consoles get theirs from elsewhere - the
/// sentry-android-ndk AAR loads `libsentry.so` by name from Java, and the console plugins ship with
/// the platform SDK - so those builds keep the original name.
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
