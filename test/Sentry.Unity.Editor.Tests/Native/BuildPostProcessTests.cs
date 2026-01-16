using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Editor.Native;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Editor.Tests.Native;

public class BuildPostProcessTests
{
    private string _testDir = null!;
    private TestLogger _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
        _logger = new TestLogger();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Test]
    public void AddPath_FileExists_AddsToList()
    {
        var paths = new List<string>();
        var testFile = Path.Combine(_testDir, "test.exe");
        File.WriteAllText(testFile, "test");

        BuildPostProcess.AddPath(paths, testFile, _logger);

        Assert.AreEqual(1, paths.Count);
        Assert.AreEqual(testFile, paths[0]);
    }

    [Test]
    public void AddPath_DirectoryExists_AddsToList()
    {
        var paths = new List<string>();
        var testSubDir = Path.Combine(_testDir, "subdir");
        Directory.CreateDirectory(testSubDir);

        BuildPostProcess.AddPath(paths, testSubDir, _logger);

        Assert.AreEqual(1, paths.Count);
        Assert.AreEqual(testSubDir, paths[0]);
    }

    [Test]
    public void AddPath_PathDoesNotExist_DoesNotAddToList()
    {
        var paths = new List<string>();
        var nonExistentPath = Path.Combine(_testDir, "nonexistent.dll");

        BuildPostProcess.AddPath(paths, nonExistentPath, _logger);

        Assert.AreEqual(0, paths.Count);
    }

    [Test]
    public void AddPath_RequiredPathMissing_LogsWarning()
    {
        var paths = new List<string>();
        var nonExistentPath = Path.Combine(_testDir, "required.dll");

        BuildPostProcess.AddPath(paths, nonExistentPath, _logger, required: true);

        Assert.AreEqual(0, paths.Count);
        Assert.IsTrue(_logger.Logs.Any(l =>
            l.logLevel == SentryLevel.Warning &&
            l.message.Contains("Required path not found")));
    }
}
