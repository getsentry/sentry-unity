using System.IO;

namespace Sentry.Unity.Editor
{
    public static class SentryPackageInfo
    {
        internal static string PackageName = "io.sentry.unity";
        internal static string PackageNameDev = "io.sentry.unity.dev";

        internal static string GetName()
        {
            var packagePath = Path.Combine("Packages", PackageName);
            if (Directory.Exists(Path.Combine(packagePath)))
            {
                return PackageName;
            }

            packagePath = Path.Combine("Packages", PackageNameDev);
            if (Directory.Exists(Path.Combine(packagePath)))
            {
                return PackageNameDev;
            }

            throw new FileNotFoundException("Failed to locate the Sentry package");
        }
    }
}
