namespace Sentry;

internal class GraphQLHttpRequestException : Exception
{
    public GraphQLHttpRequestException()
        : this(null, null)
    { }

    public GraphQLHttpRequestException(string? message)
        : this(message, null)
    { }

    public GraphQLHttpRequestException(string? message, Exception? inner)
        : base(message, inner)
    {
        if (inner != null)
        {
            HResult = inner.HResult;
        }
    }

}
