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
    internal static IReadOnlyDictionary<string, string>? Values { get; set; }

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
}
