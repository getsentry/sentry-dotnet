using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Sentry.AspNetCore
{
    public class DefaultRequestPayloadExtractor : BaseRequestPayloadExtractor
    {
        protected override bool IsSupported(HttpRequest request) => true;

        protected override object DoExtractPayLoad(HttpRequest request)
        {
            // https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/IO/StreamReader.cs#L186
            // Default parameters other than 'leaveOpen'
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024,
                // Make sure StreamReader does not close the stream:
                leaveOpen: true))
            {
                // This could be turned into async but at this point the stream should be buffered so no real value
                // A custom serializer that would take the stream and read from it into the output stream would add more value
                // as it would avoid the need to create the following (possibly huge) string
                var body = reader.ReadToEnd();
                return body.Length == 0
                    ? null
                    : body;
            }
        }
    }
}
