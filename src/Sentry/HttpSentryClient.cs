using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Protocol;

namespace Sentry
{
    ///
    public class HttpSentryClient : ISentryClient, IDisposable
    {
        ///
        public HttpSentryClient(SentryOptions options = null)
        {
        }

        ///
        public Task<SentryResponse> CaptureEventAsync(SentryEvent @event, Scope scope, CancellationToken cancellationToken = default)
            => Task.FromResult(new SentryResponse(false));

        ///
        public SentryResponse CaptureEvent(SentryEvent @event, Scope scope)
        {
            if (scope.State != null)
            {
                if (scope.State is string scopeSring)
                {
                    @event.AddTag("scope", scopeSring);
                }
                else if (scope.State is IEnumerable<KeyValuePair<string, string>> keyValStringString)
                {
                    @event.AddTags(keyValStringString);
                }
                else if (scope.State is IEnumerable<KeyValuePair<string, object>> keyValStringObject)
                {
                    @event.AddTags(keyValStringObject.Select(k => new KeyValuePair<string, string>(k.Key, k.Value.ToString())));
                }
                else
                {
                    // TODO: possible callback invocation here
                    @event.AddExtra("State of unknown type", scope.State.GetType().ToString());
                }

            }

            return new SentryResponse(false);
        }

        ///
        public void Dispose()
        {
        }
    }
}
