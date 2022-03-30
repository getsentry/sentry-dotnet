using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

#if NET5_0_OR_GREATER
using System.IO;
#endif

namespace Sentry.Http
{
    /// <summary>
    /// Provides an mechanism for reading from an <see cref="HttpContent"/> object, either synchronously or
    /// asynchronously.  Used internally by Sentry, but also useful for higher-level SDKs (such as Unity) which
    /// can override the methods to provide a custom implementation.
    /// </summary>
    public class HttpContentReader
    {
        /// <summary>
        /// Reads string content synchronously.
        /// </summary>
        /// <param name="content">The content of an HTTP message.</param>
        /// <returns>The string content from the message.</returns>
        protected internal virtual string ReadString(HttpContent content)
        {
#if NET5_0_OR_GREATER
            using var stream = content.ReadAsStream();
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
#else
            return content.ReadAsStringAsync().Result;
#endif
        }

        /// <summary>
        /// Reads string content asynchronously.
        /// </summary>
        /// <param name="content">The content of an HTTP message.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The string content from the message.</returns>
        protected internal virtual async Task<string> ReadStringAsync(HttpContent content,
            CancellationToken cancellationToken)
        {
            return await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads JSON content synchronously.
        /// </summary>
        /// <param name="content">The content of an HTTP message.</param>
        /// <returns>The JSON content from the message.</returns>
        protected internal virtual JsonElement ReadJson(HttpContent content)
        {
#if NET5_0_OR_GREATER
            using var stream = content.ReadAsStream();
            using var reader = new StreamReader(stream);
            using var document = JsonDocument.Parse(stream);

            return document.RootElement.Clone();
#else
            return ReadJsonAsync(content, default).Result;
#endif
        }

        /// <summary>
        /// Reads JSON content asynchronously.
        /// </summary>
        /// <param name="content">The content of an HTTP message.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The JSON content from the message.</returns>
        protected internal virtual async Task<JsonElement> ReadJsonAsync(HttpContent content,
            CancellationToken cancellationToken)
        {
            var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#if NET461 || NETSTANDARD2_0
            using (stream)
#else
            await using (stream.ConfigureAwait(false))
#endif
            {
                using var document = await JsonDocument.ParseAsync(stream, default, cancellationToken)
                    .ConfigureAwait(false);

                return document.RootElement.Clone();
            }
        }
    }
}
