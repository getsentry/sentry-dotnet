using Sentry.Infrastructure;

namespace Sentry.Extensibility;

#if NET8_0_OR_GREATER

/// <summary>
/// A rudimentary implementation of <see cref="ISentryStackTraceFactory"/> that simply parses the
/// string representation of the stack trace from an exception. This lacks many of the features
/// off the full <see cref="SentryStackTraceFactory"/>. However, it may be useful in AOT compiled
/// applications where the full factory is not returning a useful stack trace.
/// <remarks>
/// <para>
/// This class is currently EXPERIMENTAL
/// </para>
/// <para>
/// This factory is designed for AOT scenarios, so only available for net8.0+
/// </para>
/// </remarks>
/// </summary>
[Experimental(DiagnosticId.ExperimentalFeature)]
public partial class StringStackTraceFactory : ISentryStackTraceFactory
{
    private readonly SentryOptions _options;
    private const string FullStackTraceLinePattern = @"at (?<Module>[^\.]+)\.(?<Function>.*?) in (?<FileName>.*?):line (?<LineNo>\d+)";
    private const string StackTraceLinePattern = @"at (.+)\.(.+) \+";

#if NET9_0_OR_GREATER
    [GeneratedRegex(FullStackTraceLinePattern)]
    internal static partial Regex FullStackTraceLine { get; }
#else
    internal static readonly Regex FullStackTraceLine = FullStackTraceLineRegex();

    [GeneratedRegex(FullStackTraceLinePattern)]
    private static partial Regex FullStackTraceLineRegex();
#endif

#if NET9_0_OR_GREATER
    [GeneratedRegex(StackTraceLinePattern)]
    private static partial Regex StackTraceLine { get; }
#else
    private static readonly Regex StackTraceLine = StackTraceLineRegex();

    [GeneratedRegex(StackTraceLinePattern)]
    private static partial Regex StackTraceLineRegex();
#endif

    /// <summary>
    /// Creates a new instance of <see cref="StringStackTraceFactory"/>.
    /// </summary>
    /// <param name="options">The sentry options</param>
    public StringStackTraceFactory(SentryOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public SentryStackTrace? Create(Exception? exception = null)
    {
        _options.LogDebug("Source Stack Trace: {0}", exception?.StackTrace);

        var trace = new SentryStackTrace();
        var frames = new List<SentryStackFrame>();

        var lines = exception?.StackTrace?.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries) ?? [];
        foreach (var line in lines)
        {
            var fullMatch = FullStackTraceLine.Match(line);
            if (fullMatch.Success)
            {
                frames.Add(new SentryStackFrame()
                {
                    Module = fullMatch.Groups[1].Value,
                    Function = fullMatch.Groups[2].Value,
                    FileName = fullMatch.Groups[3].Value,
                    LineNumber = int.Parse(fullMatch.Groups[4].Value),
                });
                continue;
            }

            _options.LogDebug("Full stack frame match failed for: {0}", line);
            var lineMatch = StackTraceLine.Match(line);
            if (lineMatch.Success)
            {
                frames.Add(new SentryStackFrame()
                {
                    Module = lineMatch.Groups[1].Value,
                    Function = lineMatch.Groups[2].Value
                });
                continue;
            }

            _options.LogDebug("Stack frame match failed for: {0}", line);
            frames.Add(new SentryStackFrame()
            {
                Function = line
            });
        }

        trace.Frames = frames;
        _options.LogDebug("Created {0} with {1} frames.", "StringStackTrace", trace.Frames.Count);
        return trace.Frames.Count != 0 ? trace : null;
    }
}

#endif
