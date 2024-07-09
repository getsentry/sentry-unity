using System;
using Sentry.Extensibility;

namespace Sentry.Unity.Integrations;

internal class UnityWebExceptionFilter : IExceptionFilter
{
    internal const string Message = "Error: ConnectFailure (The requested address is not valid in this context)";

    public bool Filter(Exception ex) =>
        ex.GetType() == typeof(System.Net.WebException) &&
        ex.Message.Equals(Message);
}
