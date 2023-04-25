using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentry;
internal static class HttpHeadersExtensions
{
    internal static string GetCookies(this HttpHeaders headers)
    {
        if (headers.TryGetValues("Cookie", out var values))
        {
            return string.Join("; ", values);
        }
        return string.Empty;
    }
}
