using System.Threading;

namespace Sentry.Unity
{
    internal sealed class MainThreadData
    {
        internal int? MainThreadId { get; set; }

        public string? OperatingSystem { get; set; }

        public int? ProcessorCount { get; set; }

        public bool? SupportsVibration { get; set; }

        public string? DeviceType { get; set; }

        public string? CpuDescription { get; set; }

        public string? DeviceName { get; set; }

        public string? DeviceUniqueIdentifier { get; set; }

        public string? DeviceModel { get; set; }

        public int? SystemMemorySize { get; set; }

        public bool IsMainThread()
            => MainThreadId.HasValue && Thread.CurrentThread.ManagedThreadId == MainThreadId;
    }
}
