using System;

namespace Sentry
{
    public class SentryException : Exception
    {
        public SentryException(string message) : base(message)
        {
        }
    }
}