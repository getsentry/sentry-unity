using System;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    [Serializable]
    public sealed class SentryCliOptions : ScriptableObject
    {
        [field: SerializeField] public bool UploadSymbols { get; set; } = true;
        [field: SerializeField] public string? Auth { get; set; }
        [field: SerializeField] public string? Organization  { get; set; }
        [field: SerializeField] public string? Project { get; set; }
    }
}
