using System;
using Sentry.Extensibility;

namespace Sentry.Unity.Integrations;

internal class UnityBadGatewayExceptionFilter : IExceptionFilter
{
    internal const string Message = "Error: HTTP/1.1 502 Bad Gateway";

    public bool Filter(Exception ex) =>
        ex.GetType() == typeof(Exception) &&
        ex.Message.StartsWith(Message);
}