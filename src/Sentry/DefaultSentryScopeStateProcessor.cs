#if !NET461
using System;
#endif
using System.Collections.Generic;
using System.Linq;

namespace Sentry
{
    /// <summary>
    /// Defines the logic for applying state onto a scope.
    /// </summary>
    public class DefaultSentryScopeStateProcessor : ISentryScopeStateProcessor
    {
        /// <summary>
        /// Applies state onto a scope.
        /// </summary>
        public void Apply(Scope scope, object state)
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
                        .Where(kv => !string.IsNullOrEmpty(kv.Value as string))
                        .Select(k => new KeyValuePair<string, string>(
                            k.Key,
                            // TODO: Candidate for serialization instead. Likely Contexts is a better fit.
                            k.Value.ToString()!)));


                    break;
                }
#if !NET461
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
