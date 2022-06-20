using System;
using Sentry.Unity.Integrations;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    internal static class SentryUnityVersion
    {
        public static bool IsNewerOrEqualThan(string version, IApplication? application = null)
            => GetVersion(application) >= new Version(version);

        internal static Version? GetVersion(IApplication? application = null)
        {
            application ??= ApplicationAdapter.Instance;

            // The Unity version format looks like this: '2019.4.38f1', '2022.1.0a17' or '2022.1.1b4'
            // We're trimming going from the back to the first letter
            var unityVersion = application.UnityVersion;
            for (var i = unityVersion.Length -1; i > 0; i--)
            {
                if (!char.IsLetter(unityVersion, i))
                {
                    continue;
                }

                return new Version(unityVersion.Substring(0, i));
            }

            return new Version(unityVersion);
        }
    }
}
