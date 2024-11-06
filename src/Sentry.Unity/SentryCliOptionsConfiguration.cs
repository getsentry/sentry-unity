using UnityEngine;

namespace Sentry.Unity;

public abstract class SentryCliOptionsConfiguration : ScriptableObject
{
    public static readonly string Template =
        """
        using Sentry.Unity;

        public class {{ScriptName}} : SentryCliOptionsConfiguration
        {
            public override void Configure(SentryCliOptions cliOptions)
            {
                // Here you can programmatically modify the Sentry CLI option properties used during debug symbol upload
            }
        }
        """;

    /// <summary>
    /// Called during app build. This allows you to programmatically modify the automatic symbol upload
    /// </summary>
    public abstract void Configure(SentryCliOptions cliOptions);
}
