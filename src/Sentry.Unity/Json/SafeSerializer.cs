using System;
using System.Text.Json;
using Sentry.Extensibility;

namespace Sentry.Unity.Json
{
    internal static class SafeSerializer
    {
        /// <summary>
        /// Attempts to serialize
        /// </summary>
        /// <param name="value"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static string? SerializeSafely(object value, IDiagnosticLogger? logger = null)
        {
            if (value is string stringValue)
            {
                // Otherwise it'll return ""value""
                return stringValue;
            }
            try
            {
                return JsonSerializer.Serialize(value);
            }
            catch (Exception e)
            {
                logger?.LogError(exception:e,"Failed to serialize value of type \"{0}\"", value.GetType());
                return null;
            }
        }
    }
}
