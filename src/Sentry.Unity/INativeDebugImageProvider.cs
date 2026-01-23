using System.Collections.Generic;
using Sentry.Protocol;

namespace Sentry.Unity;

/// <summary>
/// Provides debug images from the native SDK.
/// </summary>
public interface INativeDebugImageProvider
{
    /// <summary>
    /// Gets the list of debug images from the native SDK.
    /// </summary>
    IEnumerable<DebugImage> GetDebugImages();
}
