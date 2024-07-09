using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Sentry.Unity.Editor.iOS.Tests;

public class NativeMainTests
{
    [Test]
    public void ContainsSentry_SentryAlreadyAdded_ReturnsTrue()
    {
        var main = GetFileContents("main_expected.txt");

        var containsSentry = NativeMain.ContainsSentry(main, null);

        Assert.IsTrue(containsSentry);
    }

    [Test]
    public void ContainsSentry_SentryNotAdded_ReturnsFalse()
    {
        var main = GetFileContents("main.txt");

        var containsSentry = NativeMain.ContainsSentry(main, null);

        Assert.IsFalse(containsSentry);
    }

    [Test]
    public void AddSentryToMain_SentryNotAddedTo_MatchesExpectedOutput()
    {
        var main = GetFileContents("main.txt");
        var expectedMain = GetFileContents("main_expected.txt");

        var actualMain = NativeMain.AddSentryToMain(main);

        Assert.AreEqual(expectedMain, actualMain);
    }

    [Test]
    public void AddSentryToMain_InvalidMain_ThrowsException()
    {
        var main = string.Empty;

        var assert = Assert.Throws<ArgumentException>(() => NativeMain.AddSentryToMain(main));
        Assert.AreEqual("main", assert.ParamName);
    }

    [Test]
    public void AddSentry_MainDoesNotExist_ThrowsFileNotFoundException()
    {
        var pathToMain = "Path/That/Does/Not/Exist";

        var assert = Assert.Throws<FileNotFoundException>(() => NativeMain.AddSentry(pathToMain, null));
        StringAssert.Contains("Could not find main.", assert.Message);
    }

    [Test]
    public void AddSentry_CleanMain_OutputMatchesExpected()
    {
        var expectedMain = GetFileContents("main_expected.txt");
        var workingMainPath = "temp.txt";
        File.WriteAllText(workingMainPath, GetFileContents("main.txt"));

        NativeMain.AddSentry(workingMainPath, null);
        var actualMain = File.ReadAllText(workingMainPath);

        Assert.AreEqual(expectedMain, actualMain);

        File.Delete(workingMainPath);
    }

    private string GetFileContents(string fileName)
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var mainPath = Path.Combine(assemblyPath, "TestFiles", "2019_4", fileName);

        return File.ReadAllText(mainPath);
    }
}
