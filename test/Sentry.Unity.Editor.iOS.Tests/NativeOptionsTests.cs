using System;
using System.IO;
using NUnit.Framework;

namespace Sentry.Unity.Editor.iOS.Tests;

public class NativeOptionsTests
{
    private string _testOptionsFilePath = null!;

    [SetUp]
    public void SetUp() => _testOptionsFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.m");

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testOptionsFilePath))
        {
            File.Delete(_testOptionsFilePath);
        }
    }

    [Test]
    public void CreateOptionsFile_NewSentryOptions_FileCreated()
    {
        NativeOptions.CreateFile(_testOptionsFilePath, new SentryUnityOptions());

        Assert.IsTrue(File.Exists(_testOptionsFilePath));
    }

    [Test]
    public void CreateOptionsFile_NewSentryOptions_ContainsBaseOptions()
    {
        NativeOptions.CreateFile(_testOptionsFilePath, new SentryUnityOptions());

        Assert.IsTrue(File.Exists(_testOptionsFilePath)); // Sanity check

        var options = File.ReadAllText(_testOptionsFilePath);
        StringAssert.Contains("dsn", options);
        StringAssert.Contains("debug", options);
        StringAssert.Contains("diagnosticLevel", options);
        StringAssert.Contains("maxBreadcrumbs", options);
        StringAssert.Contains("maxCacheItems", options);
        StringAssert.Contains("enableAutoSessionTracking", options);
        StringAssert.Contains("enableAppHangTracking", options);
        StringAssert.Contains("enableCaptureFailedRequests", options);
        StringAssert.Contains("sendDefaultPii", options);
        StringAssert.Contains("attachScreenshot", options);
        StringAssert.Contains("release", options);
        StringAssert.Contains("environment", options);
    }

    [Test]
    public void CreateOptionsFile_NewSentryOptions_ContainsSdkNameSetting()
    {
        NativeOptions.CreateFile(_testOptionsFilePath, new SentryUnityOptions());

        Assert.IsTrue(File.Exists(_testOptionsFilePath)); // Sanity check

        var nativeOptions = File.ReadAllText(_testOptionsFilePath);
        StringAssert.Contains("sentry.cocoa.unity", nativeOptions);
    }

    [Test]
    public void CreateOptionsFile_EnableAppHangTracking_SetsYes()
    {
        NativeOptions.CreateFile(_testOptionsFilePath, new SentryUnityOptions { EnableAppHangTracking = true });

        var nativeOptions = File.ReadAllText(_testOptionsFilePath);
        StringAssert.Contains("@\"enableAppHangTracking\": @YES", nativeOptions);
    }

    [Test]
    public void CreateOptionsFile_AppHangTrackingDisabled_SetsNo()
    {
        NativeOptions.CreateFile(_testOptionsFilePath, new SentryUnityOptions { EnableAppHangTracking = false });

        var nativeOptions = File.ReadAllText(_testOptionsFilePath);
        StringAssert.Contains("@\"enableAppHangTracking\": @NO", nativeOptions);
    }

    [Test]
    public void CreateOptionsFile_AppHangTimeout_WrittenAsSeconds()
    {
        NativeOptions.CreateFile(_testOptionsFilePath,
            new SentryUnityOptions { AppHangTimeout = System.TimeSpan.FromMilliseconds(7500) });

        var nativeOptions = File.ReadAllText(_testOptionsFilePath);
        StringAssert.Contains("@\"appHangTimeoutInterval\": @7.5", nativeOptions);
    }

    [Test]
    public void CreateOptionsFile_FilterBadGatewayEnabled_AddsFiltering()
    {
        NativeOptions.CreateFile(_testOptionsFilePath, new SentryUnityOptions { FilterBadGatewayExceptions = true });

        Assert.IsTrue(File.Exists(_testOptionsFilePath)); // Sanity check

        var nativeOptions = File.ReadAllText(_testOptionsFilePath);
        StringAssert.Contains("event.request.url containsString:@\"operate-sdk-telemetry.unity3d.com\"", nativeOptions);
    }

    [Test]
    public void CreateOptionsFile_FilterBadGatewayDisabled_DoesNotAddFiltering()
    {
        NativeOptions.CreateFile(_testOptionsFilePath, new SentryUnityOptions { FilterBadGatewayExceptions = false });

        Assert.IsTrue(File.Exists(_testOptionsFilePath)); // Sanity check

        var nativeOptions = File.ReadAllText(_testOptionsFilePath);
        StringAssert.DoesNotContain("event.request.url containsString:@\"operate-sdk-telemetry.unity3d.com\"", nativeOptions);
    }
}
