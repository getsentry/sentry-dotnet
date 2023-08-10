namespace Sentry.GraphQl;

internal class SentryGraphQlHttpFailedRequestException : Exception
{
    public SentryGraphQlHttpFailedRequestException()
        : this(null, null)
    { }

    public SentryGraphQlHttpFailedRequestException(string? message)
        : this(message, null)
    { }

    public SentryGraphQlHttpFailedRequestException(string? message, Exception? inner)
        : base(message, inner)
    {
        if (inner != null)
        {
            HResult = inner.HResult;
        }
    }

}
