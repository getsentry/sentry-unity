using System;
using Sentry.Extensibility;

namespace Sentry.Unity.Integrations
{
    public class UnityWebExceptionFilter : IExceptionFilter
    {
        public bool Filter(Exception ex) =>
            ex.GetType() == typeof(System.Net.WebException) &&
            ex.Message.Equals("Error: ConnectFailure (The requested address is not valid in this context)");
    }
}
