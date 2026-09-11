using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Sentry.Unity.Editor.Native;
using UnityEditor;

namespace Sentry.Unity.Editor.Tests.Native;

public class SwitchNativeStubTests
{
    private static string PackageRoot() => Path.GetFullPath(Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", ".."));

    private static string StubTemplatePath() =>
        Path.Combine(PackageRoot(), "Plugins", "Switch", "SentryStub~", SwitchNativeStub.StubFileName);

    [Test]
    public void Stub_ContainsEverySwitchNativeBinding()
    {
        var packageRoot = PackageRoot();
        var switchAssemblyPath = Path.Combine(packageRoot, "Runtime", "Sentry.Unity.Native.Switch.dll");
        var stubPath = StubTemplatePath();

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

    [Test]
    public void RequiredFilesFor_Switch_ProbesTheSwitchPluginDirectory()
    {
        var requiredFiles = SwitchNativeStub.RequiredFilesFor(BuildTarget.Switch);

        Assert.That(requiredFiles, Is.EquivalentTo(new[]
        {
            "Assets/Plugins/Sentry/Switch/libsentry.a",
            "Assets/Plugins/Sentry/Switch/libzstd.a"
        }));
    }

    /// <summary>
    /// One copy per target, so a project can have real native support on one Switch generation and
    /// stubs on the other.
    /// </summary>
    [Test]
    public void StubPathFor_SitsBesideTheLibrariesItReplaces()
    {
        var stubPath = SwitchNativeStub.StubPathFor(BuildTarget.Switch);

        Assert.That(stubPath, Is.EqualTo("Assets/Plugins/Sentry/Switch/sentry_native_stubs.c"));
        Assert.That(SwitchNativeStub.RequiredFilesFor(BuildTarget.Switch),
            Has.All.StartsWith(SwitchNativeStub.PluginDirectoryFor(BuildTarget.Switch)));
    }

    [Test]
    public void Targets_CoverTheSwitchFamilyOnly()
    {
        Assert.That(SwitchNativeStub.Targets, Does.Contain(BuildTarget.Switch));
        Assert.That(SwitchNativeStub.Targets, Has.All.Matches<BuildTarget>(target => target.IsSwitchFamily()));
    }

    /// <summary>
    /// Resolved by name because <c>BuildTarget.Switch2</c> does not exist on every Unity version the
    /// SDK supports.
    /// </summary>
    [Test]
    public void RequiredFilesFor_Switch2_ProbesTheSwitch2PluginDirectory()
    {
        if (!Enum.TryParse<BuildTarget>("Switch2", out var switch2))
        {
            Assert.Ignore("This Unity version predates 'BuildTarget.Switch2'.");
        }

        var requiredFiles = SwitchNativeStub.RequiredFilesFor(switch2);

        Assert.That(requiredFiles, Is.EquivalentTo(new[]
        {
            "Assets/Plugins/Sentry/Switch2/libsentry.a",
            "Assets/Plugins/Sentry/Switch2/libzstd.a"
        }));
    }
}
