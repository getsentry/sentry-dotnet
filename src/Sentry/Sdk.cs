using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sentry
{
    public class Scope
    {
        public void AddBreadcrumb(string breadcrumb) { }
        public void SetUser(string user) { }
        // ...
    }

    public static class SentryClient
    {
        private static ISentryClient _client;

        public static IDisposable Init()
        {
            var client = new HttpSentryClient();
            _client = client;
            return client;
        }

        public static string CaptureEvent(SentryEvent evt) => throw null;
        public static Task<string> CaptureEventAsync(SentryEvent evt) => throw null;

        public static void PushScope(Scope scope)
        {

        }
    }
}
