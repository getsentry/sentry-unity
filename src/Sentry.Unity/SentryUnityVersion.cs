using System;
using System.Text.RegularExpressions;
using Sentry.Unity.Integrations;

namespace Sentry.Unity;

internal static class SentryUnityVersion
{
    public static bool IsNewerOrEqualThan(string version, IApplication? application = null)
        => GetVersion(application) >= new Version(version);

    internal static Version? GetVersion(IApplication? application = null)
    {
        application ??= ApplicationAdapter.Instance;
        var unityVersion = Regex.Replace(application.UnityVersion, "^([0-9]+\\.[0-9]+\\.[0-9]+)[a-z].*$", "$1");
        return new Version(unityVersion);
    }
}
