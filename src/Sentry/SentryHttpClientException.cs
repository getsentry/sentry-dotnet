using System;

namespace Sentry
{
    internal class SentryHttpClientException: Exception
    {
        public SentryHttpClientException() : base()
        {
        }

        public SentryHttpClientException(string? message) : base(message)
        {
        }

        public SentryHttpClientException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
