using System.Text.RegularExpressions;

namespace Sentry.Internal;

internal static partial class InAppExcludeRegexes
{
    // (^|[/\\]) — anchors to start of string or after a forward/backward slash
    // ([^/\\]*libmonosgen[^/\\]*) — last segment contains libmonosgen (no slashes before/after)
    // $ — end of string
    private const string LibMonoSgenPattern = @"(^|[/\\])([^/\\]*libmonosgen[^/\\]*)$";

    // (^|[/\\]) — anchors to start of string or after a forward/backward slash
    // ([^/\\]*libxamarin[^/\\]*) — last segment contains libxamarin (no slashes before/after)
    // $ — end of string
    private const string LibXamarinPattern = @"(^|[/\\])([^/\\]*libxamarin[^/\\]*)$";

#if NET9_0_OR_GREATER
    [GeneratedRegex(LibMonoSgenPattern)]
    internal static partial Regex LibMonoSgen { get; }

    [GeneratedRegex(LibXamarinPattern)]
    internal static partial Regex LibXamarin { get; }
#elif NET8_0
    [GeneratedRegex(LibMonoSgenPattern)]
    private static partial Regex LibMonoSgenRegex();
    internal static readonly Regex LibMonoSgen = LibMonoSgenRegex();

    [GeneratedRegex(LibXamarinPattern)]
    private static partial Regex LibXamarinRegex();
    internal static readonly Regex LibXamarin = LibXamarinRegex();
#else
    internal static readonly Regex LibMonoSgen = new(LibMonoSgenPattern, RegexOptions.Compiled);
    internal static readonly Regex LibXamarin = new(LibXamarinPattern, RegexOptions.Compiled);
#endif
}

