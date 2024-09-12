using System;

namespace Sentry.Unity.Integrations;

public class ApplicationNotRespondingException : Exception
{
    internal ApplicationNotRespondingException() : base() { }
    internal ApplicationNotRespondingException(string message) : base(message) { }
    internal ApplicationNotRespondingException(string message, Exception innerException) : base(message, innerException) { }
}
