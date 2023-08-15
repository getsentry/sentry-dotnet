namespace Sentry.GraphQL.Client;

/// <summary>
/// Helper class to extract the content from GraphQL requests and responses
/// </summary>
internal static class GraphQLContentExtractor
{
    /// <summary>
    /// Extracts a <see cref="GraphQLRequest"/> from the <paramref name="request"/>
    /// </summary>
    /// <param name="request"></param>
    /// <param name="options">Sentry options</param>
    /// <returns></returns>
    internal static async Task<GraphQLRequestContent?> ExtractRequestContentAsync(HttpRequestMessage request, SentryOptions? options)
    {
        var json = await ExtractContentAsync(request?.Content, options).ConfigureAwait(false);
        return json is not null ? new GraphQLRequestContent(json, options) : null;
    }

    /// <summary>
    /// Extracts the Json text a <paramref name="response"/>
    /// </summary>
    /// <param name="response"></param>
    /// <param name="options">Sentry options</param>
    /// <returns></returns>
    internal static async Task<JsonElement?> ExtractResponseContentAsync(HttpResponseMessage response, SentryOptions? options)
    {
        var json = await ExtractContentAsync(response?.Content, options).ConfigureAwait(false);
        return (json is not null) ? JsonDocument.Parse(json).RootElement.Clone() : null;
    }

    private static void TrySeek(Stream? stream, long position)
    {
        if (stream?.CanSeek ?? false)
        {
            stream.Position = position;
        }
    }

    internal static async Task<string?> ExtractContentAsync(HttpContent? content, SentryOptions? options)
    {
        // Not to throw on code that ignores nullability warnings.
        if (content is null)
        {
            return null;
        }

        Stream contentStream;
        try
        {
            await content.LoadIntoBufferAsync().ConfigureAwait(false);
            contentStream = await content.ReadAsStreamAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            options?.LogDebug($"Unable to read GraphQL content stream: {exception.Message}");
            return null;
        }

        if (!contentStream.CanRead)
        {
            return null;
        }

        var originalPosition = (contentStream.CanSeek) ? contentStream.Position : 0;
        try
        {
            TrySeek(contentStream, 0);
            // https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/IO/StreamReader.cs#L186
            // Default parameters other than 'leaveOpen'
            using var reader = new StreamReader(contentStream, Encoding.UTF8, true,
                1024, leaveOpen: true);
            return reader.ReadToEndAsync().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            options?.LogDebug($"Unable to extract GraphQL content: {exception.Message}");
            return null;
        }
        finally
        {
            TrySeek(contentStream, originalPosition);
        }
    }
}
