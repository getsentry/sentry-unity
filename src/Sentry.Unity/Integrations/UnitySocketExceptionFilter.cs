using System;
using Sentry.Extensibility;

namespace Sentry.Unity.Integrations;

internal class UnitySocketExceptionFilter : IExceptionFilter
{
    internal const string Message = "The requested address is not valid in this context";

    public bool Filter(Exception ex) =>
        ex.GetType() == typeof(System.Net.Sockets.SocketException) &&
        ex.Message.Equals(Message);
}