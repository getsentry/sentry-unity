using NUnit.Framework;

namespace Sentry.Unity.Tests.Protocol;

public sealed class UnityTests
{
    [Test]
    public void Ctor_NoPropertyFilled_SerializesEmptyObject()
    {
        var sut = new Unity.Protocol.Unity();

        var actual = sut.ToJsonString();

        Assert.AreEqual("{\"type\":\"unity\"}", actual);
    }

    [Test]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        var sut = new Unity.Protocol.Unity
        {
            InstallMode = "DeveloperBuild",
            IsMainThread = false,
            CopyTextureSupport = "Copy3D",
            RenderingThreadingMode = "MultiThreaded",
            TargetFrameRate = "30",
            ActiveSceneName = "TestScene",
        };

        var actual = sut.ToJsonString();

        Assert.AreEqual(
            "{\"type\":\"unity\"," +
            "\"install_mode\":\"DeveloperBuild\"," +
            "\"is_main_thread\":false," +
            "\"copy_texture_support\":\"Copy3D\"," +
            "\"rendering_threading_mode\":\"MultiThreaded\"," +
            "\"target_frame_rate\":\"30\"," +
            "\"active_scene_name\":\"TestScene\"}",
            actual);
    }

    [Test]
    public void Clone_CopyValues()
    {
        var sut = new Unity.Protocol.Unity
        {
            InstallMode = "DeveloperBuild",
            IsMainThread = false,
            CopyTextureSupport = "Copy3D",
            RenderingThreadingMode = "MultiThreaded",
            TargetFrameRate = "30",
            ActiveSceneName = "TestScene"
        };

        var clone = sut.Clone();

        Assert.AreEqual(sut.InstallMode, clone.InstallMode);
        Assert.AreEqual(sut.IsMainThread, clone.IsMainThread);
        Assert.AreEqual(sut.CopyTextureSupport, clone.CopyTextureSupport);
        Assert.AreEqual(sut.RenderingThreadingMode, clone.RenderingThreadingMode);
        Assert.AreEqual(sut.TargetFrameRate, clone.TargetFrameRate);
        Assert.AreEqual(sut.ActiveSceneName, clone.ActiveSceneName);
    }

    [TestCaseSource(nameof(TestCases))]
    public void SerializeObject_TestCase_SerializesAsExpected((Unity.Protocol.Unity device, string serialized) @case)
    {
        var actual = @case.device.ToJsonString();

        Assert.AreEqual(@case.serialized, actual);
    }

    private static object[] TestCases =
    {
        new object[] { (new Unity.Protocol.Unity(), "{\"type\":\"unity\"}") },
        new object[] { (new Unity.Protocol.Unity { InstallMode = "Adhoc" }, "{\"type\":\"unity\",\"install_mode\":\"Adhoc\"}") },
        new object[] { (new Unity.Protocol.Unity { IsMainThread = false }, "{\"type\":\"unity\",\"is_main_thread\":false}") },
        new object[] { (new Unity.Protocol.Unity { CopyTextureSupport = "TextureToRT" }, "{\"type\":\"unity\",\"copy_texture_support\":\"TextureToRT\"}") },
        new object[] { (new Unity.Protocol.Unity { RenderingThreadingMode = "NativeGraphicsJobs" }, "{\"type\":\"unity\",\"rendering_threading_mode\":\"NativeGraphicsJobs\"}") },
        new object[] { (new Unity.Protocol.Unity { TargetFrameRate = "30" }, "{\"type\":\"unity\",\"target_frame_rate\":\"30\"}") },
        new object[] { (new Unity.Protocol.Unity { ActiveSceneName = "TestScene" }, "{\"type\":\"unity\",\"active_scene_name\":\"TestScene\"}") }
    };
}
