using System.Collections.Specialized;
using Sentry.Extensibility;

namespace Sentry.AspNet.Internal;

internal class SystemWebHttpRequest : IHttpRequest
{
    private readonly HttpRequest _request;

    public long? ContentLength => _request?.ContentLength;

    public string? ContentType => _request?.ContentType;

    public Stream? Body => _request?.InputStream;

    public IEnumerable<KeyValuePair<string, IEnumerable<string>>>? Form => GetFormData(_request.Form);

    public SystemWebHttpRequest(HttpRequest request) => _request = request;

    internal static IEnumerable<KeyValuePair<string, IEnumerable<string>>> GetFormData(NameValueCollection formdata)
    {
        return StripNulls(formdata.AllKeys).Select(key => new KeyValuePair<string, IEnumerable<string>>(
            key, StripNulls(formdata.GetValues(key)
            )));

        // Poorly constructed form submissions can result in null keys/values on .NET Framework.
        // See: https://github.com/getsentry/sentry-dotnet/issues/3701
        IEnumerable<string> StripNulls(IEnumerable<string>? values) => values?.Where(x => x is not null) ?? [];
    }

}
