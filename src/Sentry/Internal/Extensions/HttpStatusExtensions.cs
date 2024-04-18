namespace Sentry.Internal.Extensions;

#if !NET5_0_OR_GREATER
internal static class HttpStatusExtensions
{
    private const string HttpRequestExceptionMessage = "Response status code does not indicate success: {0}";

    /// <summary>
    /// This mimics the behaviour of <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/> for netcore3.0 and later
    /// by throwing an exception if the status code is outside the 200 range without disposing the content.
    ///
    /// See https://github.com/getsentry/sentry-dotnet/issues/2684
    /// </summary>
    /// <param name="statusCode"></param>
    /// <exception cref="HttpRequestException"></exception>
    public static void EnsureSuccessStatusCode(this HttpStatusCode statusCode)
    {
        if ((int)statusCode < 200 || (int)statusCode > 299)
        {
            throw new HttpRequestException(string.Format(
                CultureInfo.InvariantCulture,
                HttpRequestExceptionMessage,
                (int)statusCode
            ));
        }
    }
}
#endif
