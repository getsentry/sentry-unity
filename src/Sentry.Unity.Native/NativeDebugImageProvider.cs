using System.Collections.Generic;
using Sentry.Protocol;

namespace Sentry.Unity.Native;

internal class NativeDebugImageProvider : INativeDebugImageProvider
{
    public IEnumerable<DebugImage> GetDebugImages() => C.DebugImages.Value;
}
