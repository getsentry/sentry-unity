using System.Collections.Generic;

namespace Sentry.Unity.Samples
{
    /// <summary>Where the error samples run their code.</summary>
    public enum RunOn
    {
        MainThread,
        Task,
        AwaitedTask,
        Coroutine,
        Thread,
    }

    /// <summary>
    /// Every sample the menu lists, by category. Each category is a class of its own, in a file of its own, such as
    /// <see cref="ErrorSamples"/> in ErrorSamples.cs.
    /// </summary>
    public static class SampleCatalogue
    {
        public static readonly IReadOnlyList<SampleCategory> Categories = new[]
        {
            ErrorSamples.Category,
            CrashSamples.Category,
            PlatformSamples.Category,
            HangSamples.Category,
            ContextSamples.Category,
            TracingSamples.Category,
            LogAndMetricSamples.Category,
            FeedbackSamples.Category,
            SceneSamples.Category,
            ThirdPartySamples.Category,
        };

        /// <summary>Where the error samples run their code. The menu's "Run on" selector sets it.</summary>
        public static RunOn RunOn { get; set; } = RunOn.MainThread;
    }

    public sealed class SampleCategory
    {
        public string Name { get; }
        public IReadOnlyList<SampleEntry> Entries { get; }

        public SampleCategory(string name, params SampleEntry[] entries)
        {
            Name = name;
            Entries = entries;
            foreach (var entry in entries)
            {
                entry.Category = name;
            }
        }
    }
}
