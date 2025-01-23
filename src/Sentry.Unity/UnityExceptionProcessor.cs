using System;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;

namespace Sentry.Unity;

public class UnityExceptionProcessor : ISentryEventExceptionProcessor
{
    public void Process(Exception exception, SentryEvent sentryEvent)
    {
        if (exception is UnityErrorLogException ule)
        {
            sentryEvent.SentryExceptions = [ule.ToSentryException()];
            sentryEvent.SetTag("source", "log");
        }
    }
}
