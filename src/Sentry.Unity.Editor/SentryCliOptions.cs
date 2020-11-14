using System;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    [Serializable]
    public class SentryCliOptions : ScriptableObject
    {
        [field: SerializeField]
        public bool UploadSymbols { get; set; } = true;
        [field: SerializeField]
        private string Auth { get; set; }
        [field: SerializeField]
        private string Organization  { get; set; }
        [field: SerializeField]
        private string Project { get; set; }
    }
}
