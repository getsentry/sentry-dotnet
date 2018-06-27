using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Sentry.AspNetCore
{
    public class DefaultRequestPayloadExtractor : IRequestPayloadExtractor
    {
        public object ExtractPayload(HttpRequest request)
        {
            // https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/IO/StreamReader.cs#L186
            // Default parameters other than 'leaveOpen'
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024,
                // Make sure StreamReader does not close the stream
                leaveOpen: true))
            {
                var body = reader.ReadToEnd();
                return body.Length == 0
                    ? null
                    : body;
            }
        }
    }
}
