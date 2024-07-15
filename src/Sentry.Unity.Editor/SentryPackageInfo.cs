using System.IO;

namespace Sentry.Unity.Editor;

internal static class SentryPackageInfo
{
    internal static string PackageName = SentryUnityOptions.PackageName;
    internal static string PackageNameDev = SentryUnityOptions.PackageName + ".dev";

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

    internal static bool IsDevPackage => GetName() == PackageNameDev;
}
