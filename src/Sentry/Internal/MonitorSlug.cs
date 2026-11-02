using System.Text;

namespace Sentry.Internal;

/// <summary>
/// Builds monitor slugs the way Sentry's slugify_monitor_slug does, so Sentry shows the same slug.
/// </summary>
/// <remarks>
/// Also compiled into Sentry.Hangfire, which can't see Sentry's internals because it isn't strong-named.
/// </remarks>
internal static class MonitorSlug
{
    private const int MaxLength = 50;

    /// <summary>
    /// Returns the slug of <paramref name="value"/>, or <c>null</c> when nothing usable is left.
    /// </summary>
    public static string? From(string value)
    {
        string normalized;
        try
        {
            normalized = value.Normalize(NormalizationForm.FormKD);
        }
        catch (PlatformNotSupportedException)
        {
            // Invariant globalization mode can't normalize non-ASCII text
            normalized = value;
        }

        var slug = new StringBuilder(normalized.Length);
        var inSeparator = false;
        foreach (var c in normalized)
        {
            if (c is '-' or ' ' or >= '\t' and <= '\r' or >= '\x1c' and <= '\x1f')
            {
                if (!inSeparator)
                {
                    slug.Append('-');
                    inSeparator = true;
                }
            }
            else if (c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_')
            {
                slug.Append(c);
                inSeparator = false;
            }
            else if (c is >= 'A' and <= 'Z')
            {
                slug.Append(char.ToLowerInvariant(c));
                inSeparator = false;
            }
        }

        var result = slug.ToString().Trim('-', '_');
        if (result.Length > MaxLength)
        {
            result = result.Substring(0, MaxLength).TrimEnd('-');
        }
        return result.Length > 0 ? result : null;
    }
}
