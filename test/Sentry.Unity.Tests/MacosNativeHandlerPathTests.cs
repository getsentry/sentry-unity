using System.IO;
using NUnit.Framework;

namespace Sentry.Unity.Tests;

public class MacosNativeHandlerPathTests
{
    private static readonly string Bundled = Path.Combine("PlugIns", "SentryCrash.bundle", "Contents", "MacOS", "sentry-crash");

    [Test]
    public void Resolve_App_FindsTheBundleInContentsPlugIns()
    {
        var dataPath = Path.Combine("/games", "game.app", "Contents");
        var expected = Path.Combine(dataPath, Bundled);

        var actual = MacosNativeHandlerPath.Resolve(dataPath, path => path == expected, out var expectedPath);

        Assert.AreEqual(expected, actual);
        Assert.AreEqual(expected, expectedPath);
    }

    [Test]
    public void Resolve_Server_FindsTheBundleNextToTheData()
    {
        var root = Path.Combine("/games", "server");
        var dataPath = Path.Combine(root, "server_Data");
        var server = Path.Combine(root, Bundled);

        var actual = MacosNativeHandlerPath.Resolve(dataPath, path => path == server, out var expectedPath);

        Assert.AreEqual(server, actual);
        Assert.AreEqual(Path.Combine(dataPath, Bundled), expectedPath);
    }

    [Test]
    public void Resolve_Missing_ReturnsNullAndNamesTheAppPath()
    {
        var dataPath = Path.Combine("/games", "game.app", "Contents");

        var actual = MacosNativeHandlerPath.Resolve(dataPath, _ => false, out var expectedPath);

        Assert.IsNull(actual);
        Assert.AreEqual(Path.Combine(dataPath, Bundled), expectedPath);
    }
}
