using Sentry.Unity;

public class TestUnityInfo : ISentryUnityInfo
{
    public bool IL2CPP { get; set; }
    public Il2CppMethods? Il2CppMethods { get; }

    public bool TryGetFrameThreadTimings(out double gameThreadTime, out double renderThreadTime)
    {
        gameThreadTime = 0.0;
        renderThreadTime = 0.0;
        return false;
    }
}
