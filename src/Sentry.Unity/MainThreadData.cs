using System.Threading;

namespace Sentry.Unity
{
    internal sealed class MainThreadData
    {
        internal int? MainThreadId { get; set; }

        public string? OperatingSystem { get; set; }

        public bool IsMainThread()
            => MainThreadId.HasValue && Thread.CurrentThread.ManagedThreadId == MainThreadId;
    }
}
