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
            var bridgePath = Path.Combine(packageRoot, "Plugins", "iOS", "SentryNativeBridge.m");
            var noOpBridgePath = Path.Combine(packageRoot, "Plugins", "iOS", "SentryNativeBridgeNoOp.m");

            var bridgeContent = File.ReadAllText(bridgePath);
            var noOpBridgeContent = File.ReadAllText(noOpBridgePath);

            foreach (var entryPoint in entryPoints)
            {
                var pattern = $@"{entryPoint}\s*\(";

                Assert.That(Regex.IsMatch(bridgeContent, pattern),
                    $"Entry point '{entryPoint}' not found in {bridgePath}");
                Assert.That(Regex.IsMatch(noOpBridgeContent, pattern),
                    $"Entry point '{entryPoint}' not found in {noOpBridgePath}");
            }
        }
    }
}
