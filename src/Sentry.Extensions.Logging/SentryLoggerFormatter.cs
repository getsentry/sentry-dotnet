namespace Sentry.Extensions.Logging;

internal class SentryLoggerFormatter
{
    public string Invoke<TState>(TState state)
    {
        var dictionary = (state as IEnumerable<KeyValuePair<string, object>>)?.ToDictionary(i => i.Key, i => i.Value) ?? new Dictionary<string, object>();

        var entry = dictionary["{OriginalFormat}"].ToString() ?? string.Empty;

        // TBD: is this even possible?
        if (dictionary.ContainsKey("{OriginalFormat}") && dictionary.Keys.Count == 1)
        {
            return entry;
        }

        foreach (var key in dictionary.Keys)
        {
            if (key.Equals("{OriginalFormat}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parameterKey = $"{{{key}}}";

            // Regular key
            if (!key.StartsWith("@", StringComparison.OrdinalIgnoreCase))
            {
                if (dictionary.TryGetValue(key, out var obj))
                {
                    entry = entry.Replace(parameterKey, obj.ToString());
                    continue;
                }
            }

            // Object notation key
            if (!entry.Contains(parameterKey))
            {
                continue;
            }

            if (dictionary.TryGetValue(key, out var parameterObject))
            {
                var serializedParameterObject = JsonSerializer.Serialize(parameterObject);
                entry = entry.Replace(parameterKey, serializedParameterObject);
            }
        }

        return entry;
    }
}
