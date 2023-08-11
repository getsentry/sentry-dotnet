namespace Sentry.GraphQl;

/// <summary>
/// Helper class to extract the content from GraphQL requests and responses
/// </summary>
internal class GraphQlContentExtractor
{
    private readonly SentryOptions? _options;

    public GraphQlContentExtractor(SentryOptions? options)
    {
        _options = options;
    }

    /// <summary>
    /// Extracts a <see cref="GraphQLRequest"/> from the <paramref name="request"/>
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<GraphQlRequestContent?> ExtractRequestContentAsync(HttpRequestMessage request)
    {
        var json = await ExtractContentAsync(request?.Content).ConfigureAwait(false);
        return json is not null ? new GraphQlRequestContent(json) : null;
    }

    /// <summary>
    /// Extracts the Json text a <paramref name="response"/>
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    public async Task<JsonElement?> ExtractResponseContentAsync(HttpResponseMessage response)
    {
        var json = await ExtractContentAsync(response?.Content).ConfigureAwait(false);
        return (json is not null) ? JsonDocument.Parse(json).RootElement.Clone() : null;
    }

    public async Task<string?> ExtractContentAsync(HttpContent? content)
    {
        // Not to throw on code that ignores nullability warnings.
        if (content is null)
        {
            return null;
        }

        var contentStream = await content.ReadAsStreamAsync().ConfigureAwait(false);
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
            return reader.ReadToEndAsync().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            _options?.LogDebug($"Unable to extract GraphQL content: {exception.Message}");
            return null;
        }
        finally
        {
            contentStream.Position = originalPosition;
        }
    }
}
