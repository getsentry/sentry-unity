using Sentry.Unity;

public class TestUnityInfo : ISentryUnityInfo
{
    public bool IL2CPP { get; set; }
    public Il2CppMethods? Il2CppMethods { get; }
}
