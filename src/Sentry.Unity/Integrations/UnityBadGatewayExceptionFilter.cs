using System;
using Sentry.Extensibility;

namespace Sentry.Unity.Integrations
{
    internal class UnityBadGatewayExceptionFilter : IExceptionFilter
    {
        public bool Filter(Exception ex) =>
            ex.GetType() == typeof(Exception) &&
            ex.Message.StartsWith("Error: HTTP/1.1 502 Bad Gateway");
    }
}
