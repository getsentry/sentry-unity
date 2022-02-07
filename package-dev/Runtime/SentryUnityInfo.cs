using Sentry.Unity;

public class SentryUnityInfo : ISentryUnityInfo
{
    public bool IL2CPP
    {
        get =>
#if ENABLE_IL2CPP
            true;
#else
            false;
#endif
    }
}
