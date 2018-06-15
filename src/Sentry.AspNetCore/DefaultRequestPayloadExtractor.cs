using System.IO;
using Microsoft.AspNetCore.Http;

namespace Sentry.AspNetCore
{
    public class DefaultRequestPayloadExtractor : IRequestPayloadExtractor
    {
        public object ExtractPayload(HttpRequest request)
        {
            using (var reader = new StreamReader(request.Body))
            {
                var body = reader.ReadToEnd();
                return body.Length == 0
                    ? null
                    : body;
            }
        }
    }
}
