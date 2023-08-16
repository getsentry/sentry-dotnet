namespace Sentry;

internal class SentryGraphQLHttpFailedRequestException : Exception
{
    public SentryGraphQLHttpFailedRequestException()
        : this(null, null)
    { }

    public SentryGraphQLHttpFailedRequestException(string? message)
        : this(message, null)
    { }

    public SentryGraphQLHttpFailedRequestException(string? message, Exception? inner)
        : base(message, inner)
    {
        if (inner != null)
        {
            HResult = inner.HResult;
        }
    }

}
