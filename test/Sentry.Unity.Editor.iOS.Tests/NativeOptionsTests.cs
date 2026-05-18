using System.IO;
using NUnit.Framework;

namespace Sentry.Unity.Editor.iOS.Tests;

public class NativeOptionsTests
{
    [Test]
    public void CreateOptionsFile_NewSentryOptions_FileCreated()
    {
        const string testOptionsFileName = "testOptions.m";

        NativeOptions.CreateFile(testOptionsFileName, new SentryUnityOptions());

        Assert.IsTrue(File.Exists(testOptionsFileName));

        File.Delete(testOptionsFileName);
    }

    [Test]
    public void CreateOptionsFile_NewSentryOptions_ContainsBaseOptions()
    {
        const string testOptionsFileName = "testOptions.m";

        NativeOptions.CreateFile(testOptionsFileName, new SentryUnityOptions());

        Assert.IsTrue(File.Exists(testOptionsFileName)); // Sanity check

        var options = File.ReadAllText(testOptionsFileName);
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

        File.Delete(testOptionsFileName);
    }

    [Test]
    public void CreateOptionsFile_NewSentryOptions_ContainsSdkNameSetting()
    {
        const string testOptionsFileName = "testOptions.m";

        NativeOptions.CreateFile(testOptionsFileName, new SentryUnityOptions());

        Assert.IsTrue(File.Exists(testOptionsFileName)); // Sanity check

        var nativeOptions = File.ReadAllText(testOptionsFileName);
        StringAssert.Contains("sentry.cocoa.unity", nativeOptions);

        File.Delete(testOptionsFileName);
    }

    [Test]
    public void CreateOptionsFile_EnableAppHangTracking_SetsYes()
    {
        const string testOptionsFileName = "testOptions.m";

        NativeOptions.CreateFile(testOptionsFileName, new SentryUnityOptions { EnableAppHangTracking = true });

        var nativeOptions = File.ReadAllText(testOptionsFileName);
        StringAssert.Contains("@\"enableAppHangTracking\": @YES", nativeOptions);

        File.Delete(testOptionsFileName);
    }

    [Test]
    public void CreateOptionsFile_AppHangTrackingDisabled_SetsNo()
    {
        const string testOptionsFileName = "testOptions.m";

        NativeOptions.CreateFile(testOptionsFileName, new SentryUnityOptions { EnableAppHangTracking = false });

        var nativeOptions = File.ReadAllText(testOptionsFileName);
        StringAssert.Contains("@\"enableAppHangTracking\": @NO", nativeOptions);

        File.Delete(testOptionsFileName);
    }

    [Test]
    public void CreateOptionsFile_AnrTimeout_WrittenAsSeconds()
    {
        const string testOptionsFileName = "testOptions.m";

        NativeOptions.CreateFile(testOptionsFileName,
            new SentryUnityOptions { AnrTimeout = System.TimeSpan.FromMilliseconds(7500) });

        var nativeOptions = File.ReadAllText(testOptionsFileName);
        StringAssert.Contains("@\"appHangTimeoutInterval\": @7.5", nativeOptions);

        File.Delete(testOptionsFileName);
    }

    [Test]
    public void CreateOptionsFile_FilterBadGatewayEnabled_AddsFiltering()
    {
        const string testOptionsFileName = "testOptions.m";

        NativeOptions.CreateFile(testOptionsFileName, new SentryUnityOptions { FilterBadGatewayExceptions = true });

        Assert.IsTrue(File.Exists(testOptionsFileName)); // Sanity check

        var nativeOptions = File.ReadAllText(testOptionsFileName);
        StringAssert.Contains("event.request.url containsString:@\"operate-sdk-telemetry.unity3d.com\"", nativeOptions);

        File.Delete(testOptionsFileName);
    }

    [Test]
    public void CreateOptionsFile_FilterBadGatewayDisabled_DoesNotAddFiltering()
    {
        const string testOptionsFileName = "testOptions.m";

        NativeOptions.CreateFile(testOptionsFileName, new SentryUnityOptions { FilterBadGatewayExceptions = false });

        Assert.IsTrue(File.Exists(testOptionsFileName)); // Sanity check

        var nativeOptions = File.ReadAllText(testOptionsFileName);
        StringAssert.DoesNotContain("event.request.url containsString:@\"operate-sdk-telemetry.unity3d.com\"", nativeOptions);

        File.Delete(testOptionsFileName);
    }
}
