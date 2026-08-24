namespace Sentry.Internal;

/// <summary>
/// Identifies logger names that belong to the Sentry SDK itself, so the logging integrations can
/// skip them rather than feeding the SDK's own output back into Sentry.
/// </summary>
/// <remarks>
/// The <c>PackageRoots</c> half of this class is generated from <c>.craft.yml</c> - see the
/// <c>GenerateSentrySdkNamespaces</c> target in <c>Sentry.csproj</c>.
/// </remarks>
internal static partial class SentrySdkNamespaces
{
    private const string Prefix = "Sentry";

    /// <summary>
    /// The core <c>Sentry</c> package can't be matched by prefix the way the other packages can:
    /// all of its types live directly under <c>Sentry.</c>, and so does application code that
    /// happens to use that namespace (<c>Sentry.Samples.*</c>, <c>Sentry.MyApp.*</c>). Matching on
    /// <c>Sentry.</c> would therefore silently discard the user's own logs.
    ///
    /// Core has no logging dependency of its own, so the only name it ever logs under is the one
    /// <c>MelDiagnosticLogger</c> (in Sentry.Extensions.Logging) gets from
    /// <c>ILogger&lt;ISentryClient&gt;</c>. Matching that exactly is enough.
    /// </summary>
    private static readonly string[] CoreNames = { Prefix, "Sentry.ISentryClient" };

    /// <summary>
    /// Whether <paramref name="loggerName"/> - a Serilog <c>SourceContext</c>, a
    /// <c>Microsoft.Extensions.Logging</c> category, or an equivalent from another framework -
    /// belongs to the Sentry SDK.
    /// </summary>
    internal static bool IsSentrySdk(string? loggerName)
    {
        if (loggerName is null || !loggerName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var name in CoreNames)
        {
            if (string.Equals(loggerName, name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        foreach (var root in PackageRoots)
        {
            // Either the package's own namespace, or something nested inside it. Comparing the
            // character after the root keeps a package named e.g. 'Sentry.Maui' from matching an
            // unrelated 'Sentry.MauiSomething'.
            if (loggerName.StartsWith(root, StringComparison.Ordinal) &&
                (loggerName.Length == root.Length || loggerName[root.Length] == '.'))
            {
                return true;
            }
        }

        return false;
    }
}
