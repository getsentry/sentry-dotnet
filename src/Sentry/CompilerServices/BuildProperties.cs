using Sentry.Internal.Extensions;

namespace Sentry.CompilerServices;

/// <summary>
/// This class is not meant for external usage
/// </summary>
public static class BuildProperties
{
    /// <summary>
    /// The Build Variables generated from you csproj file and initialized by the Sentry Source Generated Module Initializer
    /// </summary>
    public static IReadOnlyDictionary<string, string>? Values { get; private set; }

    /// <summary>
    /// This is called by a Sentry Source-Generator module initializers to help us determine things like
    ///     Is your app AOT
    ///     Has your application been trimmed
    ///     What build configuration is being used
    /// </summary>
    /// <param name="properties"></param>
    public static void Initialize(Dictionary<string, string> properties)
    {
        Values ??= properties.AsReadOnly();
    }

    /// <summary>
    /// Tries to retrieve a boolean value from the build properties.
    /// </summary>
    /// <param name="key">The item key</param>
    /// <param name="value">The value (if any)</param>
    /// <returns>True if the key was found, false otherwise</returns>
    public static bool TryGetBoolean(string key, out bool value)
    {
        value = false;
        if (!(Values?.TryGetValue(key, out var foundValue) ?? false))
        {
            return false;
        }

        if (!bool.TryParse(foundValue, out var result))
        {
            return false;
        }

        value = result;
        return true;
    }
}
