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
    /// The stub is written next to the libraries it stands in for, one copy per target, so a project
    /// can have real native support on one Switch generation and stubs on the other.
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
    /// The stamp is the only thing that tells a copy in someone's project that it is behind. Without
    /// it, a stub written before a binding was added stays put and the Switch build fails to link.
    /// </summary>
    [Test]
    public void Stub_CarriesAVersionStamp()
    {
        var version = SwitchNativeStub.VersionOf(File.ReadAllText(StubTemplatePath()));

        Assert.That(version, Is.Not.Null,
            $"The stub template must carry a '{SwitchNativeStub.VersionMarker}' line in its header.");
        Assert.That(version, Does.Match(@"^\d+$"), "The stub version must be a plain number.");
    }

    [Test]
    public void VersionOf_IgnoresAStubThatCarriesNoStamp()
    {
        Assert.That(SwitchNativeStub.VersionOf("/* no stamp here */\nint main(void) { return 0; }"), Is.Null);
    }

    /// <summary>
    /// Switch 2 is resolved by name because <c>BuildTarget.Switch2</c> does not exist on the Unity versions the
    /// SDK still supports, so this parses the member instead of referencing it and skips where it is unavailable.
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
