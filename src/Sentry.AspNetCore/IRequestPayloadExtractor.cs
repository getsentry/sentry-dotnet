using Microsoft.AspNetCore.Http;

namespace Sentry.AspNetCore
{
    public interface IRequestPayloadExtractor
    {
        object ExtractPayload(HttpRequest request);
    }
}
