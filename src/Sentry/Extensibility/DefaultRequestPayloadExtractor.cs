using System.IO;
using System.Text;

namespace Sentry.Extensibility
{
    /// <summary>
    /// Default request payload extractor that will read the body as a string.
    /// </summary>
    public class DefaultRequestPayloadExtractor : BaseRequestPayloadExtractor
    {
        /// <summary>
        /// Whether the <see cref="IHttpRequest"/> is supported.
        /// </summary>
        protected override bool IsSupported(IHttpRequest request) => true;

        /// <summary>
        /// Extracts the request body of the <see cref="IHttpRequest"/> as a string.
        /// </summary>
        protected override object? DoExtractPayLoad(IHttpRequest request)
        {
            if(request.Body is null)
            {
                return null;
            }
            // https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/IO/StreamReader.cs#L186
            // Default parameters other than 'leaveOpen'
            using var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024,
                // Make sure StreamReader does not close the stream:
                leaveOpen: true);

            // This can't be turned into async because it's called by sync API, i.e: _logger.LogError()
            // But at this point the stream should already be buffered: Model binding happened,
            // request is buffered so data is still in memory, no blocking call is done below.
            // A custom serializer that would take the stream and read from it into the output stream would add more value
            // as it would avoid the need to create the following (possibly huge) string
            // Note: Using ReadToEndAsync instead of ReadToEnd because in ASP.NET Core 3 sync calls will throw.
            var body = reader.ReadToEndAsync().GetAwaiter().GetResult();
            return body.Length != 0
                ? body
                : null;
        }
    }
}
