using Sentry.Extensibility;

namespace Sentry;

internal static class GraphQLContentExtractor
{
    internal static async Task<GraphQLRequestContent?> ExtractRequestContentAsync(HttpRequestMessage request, SentryOptions? options)
    {
        var json = await ExtractContentAsync(request?.Content, options).ConfigureAwait(false);
        return json is not null ? new GraphQLRequestContent(json, options) : null;
    }

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

    private static async Task<string?> ExtractContentAsync(HttpContent? content, SentryOptions? options)
    {
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
#if NETFRAMEWORK
            // On .NET Framework a positive buffer size needs to be specified
            using var reader = new StreamReader(contentStream, Encoding.UTF8, true, 128, true);
#else
            // For .NET Core Apps, setting the buffer size to -1 uses the default buffer size
            using var reader = new StreamReader(contentStream, Encoding.UTF8, true, -1, true);
#endif
            return await reader.ReadToEndAsync().ConfigureAwait(false);
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
