using System;
using System.Text.RegularExpressions;
using Sentry.Unity.Integrations;

namespace Sentry.Unity.Editor;

internal static class SentryUnityVersion
{
    public static bool IsNewerOrEqualThan(string version, IApplication? application = null)
        => GetVersion(application) >= new Version(version);

    internal static Version? GetVersion(IApplication? application = null)
    {
        application ??= ApplicationAdapter.Instance;
        // The Unity version format looks like this: '2019.4.38f1', '2022.1.0a17' or '2022.1.1b4',
        // but Version() expects only the numerical parts, e.g. `2021.1.0`
        var unityVersion = Regex.Replace(application.UnityVersion, "^([0-9]+\\.[0-9]+\\.[0-9]+)[a-z].*$", "$1");
        return new Version(unityVersion);
    }
}
