using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Sentry.Unity.iOS;

namespace Sentry.Unity.Editor.iOS.Tests
{
    public class SentryNativeBridgeTests
    {
        [Test]
        public void EntryPoints_ExistInNativeBridges()
        {
            var entryPoints = typeof(SentryCocoaBridgeProxy)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.GetCustomAttribute<DllImportAttribute>() != null)
                .Select(method => method.GetCustomAttribute<DllImportAttribute>().EntryPoint)
                .ToList();

            Assert.That(entryPoints, Is.Not.Empty); // Sanity check

            var packageRoot = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", ".."));
            var bridgePaths = new[] { "SentryNativeBridge.m", "SentryNativeBridgeNoOp.m", "SentryNativeBridgeExternal.m" }
                .Select(name => Path.Combine(packageRoot, "Plugins", "iOS", name));

            foreach (var bridgePath in bridgePaths)
            {
                var bridgeContent = File.ReadAllText(bridgePath);
                foreach (var entryPoint in entryPoints)
                {
                    Assert.That(Regex.IsMatch(bridgeContent, $@"{entryPoint}\s*\("),
                        $"Entry point '{entryPoint}' not found in {bridgePath}");
                }
            }
        }
    }
}
