using System;
using System.Collections.Generic;
using System.Linq;

namespace Sentry.Protocol
{
    /// <summary>
    /// Default implementation of <see cref="ISentryScopeStateProcessor"/>.
    /// </summary>
    public class DefaultSentryScopeStateProcessor : ISentryScopeStateProcessor
    {
        /// <inheritdoc />
        public void Apply(BaseScope scope, object state)
        {
            switch (state)
            {
                case string scopeString:
                    // TODO: find unique key to support multiple single-string scopes
                    scope.SetTag("scope", scopeString);
                    break;
                case IEnumerable<KeyValuePair<string, string>> keyValStringString:
                    scope.SetTags(keyValStringString
                        .Where(kv => !string.IsNullOrEmpty(kv.Value)));
                    break;
                case IEnumerable<KeyValuePair<string, object>> keyValStringObject:
                {
                    scope.SetTags(keyValStringObject
                        .Select(k => new KeyValuePair<string, string>(
                            k.Key,
                            k.Value?.ToString()))
                        .Where(kv => !string.IsNullOrEmpty(kv.Value)));

                    break;
                }
#if HAS_VALUE_TUPLE
                case ValueTuple<string, string> tupleStringString:
                    if (!string.IsNullOrEmpty(tupleStringString.Item2))
                    {
                        scope.SetTag(tupleStringString.Item1, tupleStringString.Item2);
                    }
                    break;
#endif
                default:
                    scope.SetExtra("state", state);
                    break;
            }
        }
    }
}
