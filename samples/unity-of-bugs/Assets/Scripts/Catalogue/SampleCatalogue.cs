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
    /// Every sample the menu lists and a launch argument can run, by category. Each category is a class of its own,
    /// in a file of its own, such as <see cref="ErrorSamples"/> in ErrorSamples.cs. Each entry there is a
    /// <c>new SampleEntry("category/name", ...</c> on a line of its own: tools read the IDs from them.
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

        /// <summary>Set from the <c>run_id</c> launch argument. Logs and metrics carry it as the <c>sample.run_id</c> attribute.</summary>
        public static string RunId { get; set; }

        public static IEnumerable<SampleEntry> All
        {
            get
            {
                foreach (var category in Categories)
                {
                    foreach (var entry in category.Entries)
                    {
                        yield return entry;
                    }
                }
            }
        }

        public static SampleEntry Find(string id)
        {
            foreach (var entry in All)
            {
                if (entry.Id == id)
                {
                    return entry;
                }
            }

            return null;
        }
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
