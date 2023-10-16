namespace Sentry;

/// <summary>
/// Defines the logic for applying state onto a scope.
/// </summary>
public class DefaultSentryScopeStateProcessor : ISentryScopeStateProcessor
{
    private static readonly char[] TrimFilter = { '{', '}' };

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
                        k.Key.Trim(TrimFilter),
                        // TODO: Candidate for serialization instead. Likely Contexts is a better fit.
                        k.Value.ToString()!)));
                break;
            }
#if !NETFRAMEWORK
            case ValueTuple<string, string> tupleStringString:
                if (!string.IsNullOrEmpty(tupleStringString.Item2))
                {
                    scope.SetTag(tupleStringString.Item1, tupleStringString.Item2);
                }
                break;
#endif
            default:
                scope.SetData("state", state);
                break;
        }
    }
}
