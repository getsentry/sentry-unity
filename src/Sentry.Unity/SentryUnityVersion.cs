using System;
using System.Text.RegularExpressions;
using Sentry.Unity.Integrations;

namespace Sentry.Unity;

internal static class SentryUnityVersion
{
    private static IApplication? Application;
    private static Version? Version;

    public static bool IsNewerOrEqualThan(string version, IApplication? application = null)
        => GetVersion(application) >= new Version(version);

    internal static Version GetVersion(IApplication? application = null)
    {
        if (Version is not null)
        {
            return Version;
        }

        Application ??= application ?? ApplicationAdapter.Instance;

        // The Unity version format looks like this: '2019.4.38f1', '2022.1.0a17' or '2022.1.1b4',
        // but Version() expects only the numerical parts, e.g. `2021.1.0`
        var unityVersion = Regex.Replace(Application.UnityVersion, "^([0-9]+\\.[0-9]+\\.[0-9]+)[a-z].*$", "$1");
        Version = new Version(unityVersion);
        return Version;
    }
}
