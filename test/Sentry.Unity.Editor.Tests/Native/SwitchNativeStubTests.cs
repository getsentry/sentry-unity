using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Sentry.Unity.Editor.Tests.Native;

public class SwitchNativeStubTests
{
    [Test]
    public void Stub_ContainsEverySwitchNativeBinding()
    {
        var packageRoot = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", ".."));
        var switchAssemblyPath = Path.Combine(packageRoot, "Runtime", "Sentry.Unity.Native.Switch.dll");
        var stubPath = Path.Combine(packageRoot, "Plugins", "Switch", "sentry_native_stubs.c");

        Assert.That(File.Exists(switchAssemblyPath), Is.True, $"Switch assembly not found at {switchAssemblyPath}");
        Assert.That(File.Exists(stubPath), Is.True, $"Switch stubs not found at {stubPath}");

        var switchAssembly = Assembly.LoadFrom(switchAssemblyPath);
        var entryPoints = switchAssembly
            .GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
            .Select(method => (Method: method, Import: method.GetCustomAttribute<DllImportAttribute>()))
            .Where(binding => binding.Import?.Value == "__Internal")
            .Select(binding => binding.Import!.EntryPoint is { Length: > 0 } entryPoint
                ? entryPoint
                : binding.Method.Name)
            .Distinct()
            .ToList();

        Assert.That(entryPoints, Is.Not.Empty);

        var stubContent = File.ReadAllText(stubPath);
        foreach (var entryPoint in entryPoints)
        {
            Assert.That(Regex.IsMatch(stubContent, $@"\b{Regex.Escape(entryPoint)}\s*\("), Is.True,
                $"Switch binding '{entryPoint}' not found in {stubPath}");
        }
    }
}
