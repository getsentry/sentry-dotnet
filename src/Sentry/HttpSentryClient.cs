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
            // TODO: Consider multiple events being sent with the same scope:
            // Wherever this code will end up, it should evaluate only once
            if (scope.States != null)
            {
                foreach (var state in scope.States)
                {
                    if (state is string scopeString)
                    {
                        @event.SetTag("scope", scopeString);
                    }
                    else if (state is IEnumerable<KeyValuePair<string, string>> keyValStringString)
                    {
                        @event.SetTags(keyValStringString);
                    }
                    else if (state is IEnumerable<KeyValuePair<string, object>> keyValStringObject)
                    {
                        @event.SetTags(keyValStringObject.Select(k => new KeyValuePair<string, string>(k.Key, k.Value.ToString())));
                    }
                    else
                    {
                        // TODO: possible callback invocation here
                        @event.SetExtra("State of unknown type", state.GetType().ToString());
                    }
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
