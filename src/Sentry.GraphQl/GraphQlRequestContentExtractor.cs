namespace Sentry.GraphQl;

/// <summary>
/// Can extract GraphQL requests from the HttpRequestMessage prior to sending
/// </summary>
internal class GraphQlRequestContentExtractor
{
    private readonly SentryOptions? _options;

    public GraphQlRequestContentExtractor(SentryOptions? options)
    {
        _options = options;
    }

    /// <summary>
    /// Extracts a <see cref="GraphQLRequest"/> from the <paramref name="request"/>
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<GraphQlRequestContent?> ExtractContent(HttpRequestMessage request)
    {
        // Not to throw on code that ignores nullability warnings.
        if (request.IsNull() || request.Content is not {} requestContent)
        {
            return null;
        }

        var contentStream = await requestContent.ReadAsStreamAsync().ConfigureAwait(false);
        if (!contentStream.CanSeek || !contentStream.CanRead)
        {
            return null;
        }

        var originalPosition = contentStream.Position;
        try
        {
            contentStream.Position = 0;

            // https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/IO/StreamReader.cs#L186
            // Default parameters other than 'leaveOpen'
            using var reader = new StreamReader(contentStream, Encoding.UTF8, true,
                1024, leaveOpen: true);
            var json = reader.ReadToEndAsync().GetAwaiter().GetResult();
            return new GraphQlRequestContent(json);
        }
        catch (Exception exception)
        {
            _options?.LogDebug($"Unable to extract GraphQL Request {exception.Message}");
            return null;
        }
        finally
        {
            contentStream.Position = originalPosition;
        }
    }
}
