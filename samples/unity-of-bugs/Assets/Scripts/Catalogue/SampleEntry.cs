using System;
using System.Collections;

namespace Sentry.Unity.Samples
{
    /// <summary>
    /// What a sample needs to run. Unmet requirements grey the sample out in the menu.
    /// </summary>
    [Flags]
    public enum SampleRequirements
    {
        None = 0,
        IL2CPP = 1 << 0,
        Android = 1 << 1,
        IOS = 1 << 2,
        AndroidOrIOS = 1 << 3,
        DOTween = 1 << 4,
        UniTask = 1 << 5,
    }

    /// <summary>
    /// One sample in the catalogue: what it is, what it needs, and the code it runs.
    /// </summary>
    public sealed class SampleEntry
    {
        /// <summary>A stable <c>category/name</c> ID, such as <c>crashes/native-c</c>. Launch arguments name samples by it.</summary>
        public string Id { get; }

        /// <summary>The category the menu lists the sample under.</summary>
        public string Category { get; internal set; }

        public string Title { get; }

        /// <summary>One line on what the sample does and what arrives in Sentry.</summary>
        public string Description { get; }

        public SampleRequirements Requirements { get; }

        /// <summary>The sample ends the app. Its event is sent on the next launch.</summary>
        public bool Crashes { get; }

        /// <summary>The sample needs someone at the screen, so a launch argument can't run it.</summary>
        public bool ManualOnly { get; }

        private readonly Action _action;
        private readonly Func<IEnumerator> _routine;

        public SampleEntry(string id, string title, string description, Action run,
            SampleRequirements requirements = SampleRequirements.None, bool crashes = false, bool manualOnly = false)
            : this(id, title, description, requirements, crashes, manualOnly)
        {
            _action = run ?? throw new ArgumentNullException(nameof(run));
        }

        public SampleEntry(string id, string title, string description, Func<IEnumerator> run,
            SampleRequirements requirements = SampleRequirements.None, bool crashes = false, bool manualOnly = false)
            : this(id, title, description, requirements, crashes, manualOnly)
        {
            _routine = run ?? throw new ArgumentNullException(nameof(run));
        }

        private SampleEntry(string id, string title, string description,
            SampleRequirements requirements, bool crashes, bool manualOnly)
        {
            Id = id;
            Title = title;
            Description = description;
            Requirements = requirements;
            Crashes = crashes;
            ManualOnly = manualOnly;
        }

        /// <summary>Why the sample can't run here, or null when it can.</summary>
        public string UnmetReason
        {
            get
            {
                var reason = UnmetRequirement(Requirements);
                if (reason == null && Crashes && IsEditor)
                {
                    reason = "Crashes the Editor: run it in a player";
                }

                return reason;
            }
        }

        /// <summary>
        /// The sample's work, done as the returned routine is stepped: an action runs on the first step.
        /// </summary>
        public IEnumerator Run() => _routine != null ? _routine() : RunAction();

        private IEnumerator RunAction()
        {
            _action();
            yield break;
        }

        private static string UnmetRequirement(SampleRequirements requirements)
        {
            if (Has(requirements, SampleRequirements.Android) && !IsAndroidPlayer)
            {
                return "Android only";
            }

            if (Has(requirements, SampleRequirements.IOS) && !IsIOSPlayer)
            {
                return "iOS only";
            }

            if (Has(requirements, SampleRequirements.AndroidOrIOS) && !IsAndroidPlayer && !IsIOSPlayer)
            {
                return "Android or iOS only";
            }

            if (Has(requirements, SampleRequirements.IL2CPP) && !IsIL2CPP)
            {
                return "Needs IL2CPP";
            }

            if (Has(requirements, SampleRequirements.DOTween) && !HasDOTween)
            {
                return "Needs DOTween";
            }

            if (Has(requirements, SampleRequirements.UniTask) && !HasUniTask)
            {
                return "Needs UniTask";
            }

            return null;
        }

        private static bool Has(SampleRequirements requirements, SampleRequirements flag) => (requirements & flag) != 0;

        // Fields rather than constants, so the compiler doesn't flag the branches a platform never takes.
#if UNITY_EDITOR
        private static readonly bool IsEditor = true;
#else
        private static readonly bool IsEditor = false;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private static readonly bool IsAndroidPlayer = true;
#else
        private static readonly bool IsAndroidPlayer = false;
#endif

#if UNITY_IOS && !UNITY_EDITOR
        private static readonly bool IsIOSPlayer = true;
#else
        private static readonly bool IsIOSPlayer = false;
#endif

#if ENABLE_IL2CPP
        private static readonly bool IsIL2CPP = true;
#else
        private static readonly bool IsIL2CPP = false;
#endif

#if SENTRY_HAS_DOTWEEN
        private static readonly bool HasDOTween = true;
#else
        private static readonly bool HasDOTween = false;
#endif

#if SENTRY_HAS_UNITASK
        private static readonly bool HasUniTask = true;
#else
        private static readonly bool HasUniTask = false;
#endif
    }
}
