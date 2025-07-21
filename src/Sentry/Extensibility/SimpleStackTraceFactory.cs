using System;
using System.Collections.Generic;

namespace Sentry.Extensibility;

/// <summary>
/// A rudimentary implementation of <see cref="ISentryStackTraceFactory"/> that simply parses the
/// string representation of the stack trace from an exception. This lacks many of the features
/// off the full <see cref="SentryStackTraceFactory"/>, however it may be useful in AOT compiled
/// applications where the full factory is not returning a useful stack trace.
/// <remarks>SimpleStackTraceFactory is currently EXPERIMENTAL.</remarks>
/// </summary>
public partial class SimpleStackTraceFactory : ISentryStackTraceFactory
{
    private readonly SentryOptions _options;
    private const string FullStackTraceLinePattern = @"at (?<Module>[^\.]+)\.(?<Function>.*?) in (?<FileName>.*?):line (?<LineNo>\d+)";
    private const string StackTraceLinePattern = @"at (.+)\.(.+) \+";

#if NET9_0_OR_GREATER
    [GeneratedRegex(FullStackTraceLinePattern)]
    private static partial Regex FullStackTraceLine { get; }
#elif NET8_0
     private static readonly Regex FullStackTraceLine = FullStackTraceLineRegex();

     [GeneratedRegex(FullStackTraceLinePattern)]
     private static partial Regex FullStackTraceLineRegex();
#else
    private static readonly Regex FullStackTraceLine = new (FullStackTraceLinePattern, RegexOptions.Compiled);
#endif

#if NET9_0_OR_GREATER
    [GeneratedRegex(StackTraceLinePattern)]
    private static partial Regex StackTraceLine { get; }
#elif NET8_0
     private static readonly Regex StackTraceLine = StackTraceLineRegex();

     [GeneratedRegex(StackTraceLinePattern)]
     private static partial Regex StackTraceLineRegex();
#else
    private static readonly Regex StackTraceLine = new (StackTraceLinePattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Creates a new instance of <see cref="SimpleStackTraceFactory"/>.
    /// </summary>
    /// <param name="options">The sentry options</param>
    public SimpleStackTraceFactory(SentryOptions options)
    {
        _options = options;
    }

    /// <inheritdoc cref="ISentryStackTraceFactory.Create"/>
    public SentryStackTrace? Create(Exception? exception = null)
    {
        _options.LogDebug("Source Stack Trace: {0}", exception?.StackTrace);

        var trace = new SentryStackTrace();
        var frames = new List<SentryStackFrame>();

        var newlines = new[] { Environment.NewLine };
        var lines = exception?.StackTrace?.Split(newlines, StringSplitOptions.RemoveEmptyEntries) ?? [];
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
        return trace;
    }
}
