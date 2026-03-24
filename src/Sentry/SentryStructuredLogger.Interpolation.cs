#if NET6_0_OR_GREATER
using Sentry.Internal;

#pragma warning disable CS1572
#pragma warning disable CS1573
#pragma warning disable CS1587
#pragma warning disable CS1734
#pragma warning disable RCS1263

namespace Sentry;

public abstract partial class SentryStructuredLogger
{
    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Trace"/>, when enabled and sampled.
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    public void LogTrace([InterpolatedStringHandlerArgument("")] ref LogInterpolatedStringHandler handler, [CallerArgumentExpression(nameof(handler))] string template = "")
    {
        if (handler.ShouldLog)
        {
            CaptureLog(SentryLogLevel.Trace, handler.GetMessageAndClear(), SanitizeTemplate(template), handler.GetParametersAndClear(), null);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Trace"/>, when enabled and sampled.
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    public void LogTrace(Action<SentryLog> configureLog, [InterpolatedStringHandlerArgument("")] ref LogInterpolatedStringHandler handler, [CallerArgumentExpression(nameof(handler))] string template = "")
    {
        if (handler.ShouldLog)
        {
            CaptureLog(SentryLogLevel.Trace, handler.GetMessageAndClear(), SanitizeTemplate(template), handler.GetParametersAndClear(), configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Debug"/>, when enabled and sampled.
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    public void LogDebug([InterpolatedStringHandlerArgument("")] ref LogInterpolatedStringHandler handler, [CallerArgumentExpression(nameof(handler))] string template = "")
    {
        if (handler.ShouldLog)
        {
            CaptureLog(SentryLogLevel.Debug, handler.GetMessageAndClear(), SanitizeTemplate(template), handler.GetParametersAndClear(), null);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Debug"/>, when enabled and sampled.
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    public void LogDebug(Action<SentryLog> configureLog, [InterpolatedStringHandlerArgument("")] ref LogInterpolatedStringHandler handler, [CallerArgumentExpression(nameof(handler))] string template = "")
    {
        if (handler.ShouldLog)
        {
            CaptureLog(SentryLogLevel.Debug, handler.GetMessageAndClear(), SanitizeTemplate(template), handler.GetParametersAndClear(), configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Info"/>, when enabled and sampled.
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    public void LogInfo([InterpolatedStringHandlerArgument("")] ref LogInterpolatedStringHandler handler, [CallerArgumentExpression(nameof(handler))] string template = "")
    {
        if (handler.ShouldLog)
        {
            CaptureLog(SentryLogLevel.Info, handler.GetMessageAndClear(), SanitizeTemplate(template), handler.GetParametersAndClear(), null);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Info"/>, when enabled and sampled.
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    public void LogInfo(Action<SentryLog> configureLog, [InterpolatedStringHandlerArgument("")] ref LogInterpolatedStringHandler handler, [CallerArgumentExpression(nameof(handler))] string template = "")
    {
        if (handler.ShouldLog)
        {
            CaptureLog(SentryLogLevel.Info, handler.GetMessageAndClear(), SanitizeTemplate(template), handler.GetParametersAndClear(), configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Warning"/>, when enabled and sampled.
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    public void LogWarning([InterpolatedStringHandlerArgument("")] ref LogInterpolatedStringHandler handler, [CallerArgumentExpression(nameof(handler))] string template = "")
    {
        if (handler.ShouldLog)
        {
            CaptureLog(SentryLogLevel.Warning, handler.GetMessageAndClear(), SanitizeTemplate(template), handler.GetParametersAndClear(), null);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Warning"/>, when enabled and sampled.
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    public void LogWarning(Action<SentryLog> configureLog, [InterpolatedStringHandlerArgument("")] ref LogInterpolatedStringHandler handler, [CallerArgumentExpression(nameof(handler))] string template = "")
    {
        if (handler.ShouldLog)
        {
            CaptureLog(SentryLogLevel.Warning, handler.GetMessageAndClear(), SanitizeTemplate(template), handler.GetParametersAndClear(), configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Error"/>, when enabled and sampled.
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    public void LogError([InterpolatedStringHandlerArgument("")] ref LogInterpolatedStringHandler handler, [CallerArgumentExpression(nameof(handler))] string template = "")
    {
        if (handler.ShouldLog)
        {
            CaptureLog(SentryLogLevel.Error, handler.GetMessageAndClear(), SanitizeTemplate(template), handler.GetParametersAndClear(), null);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Error"/>, when enabled and sampled.
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    public void LogError(Action<SentryLog> configureLog, [InterpolatedStringHandlerArgument("")] ref LogInterpolatedStringHandler handler, [CallerArgumentExpression(nameof(handler))] string template = "")
    {
        if (handler.ShouldLog)
        {
            CaptureLog(SentryLogLevel.Error, handler.GetMessageAndClear(), SanitizeTemplate(template), handler.GetParametersAndClear(), configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Fatal"/>, when enabled and sampled.
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    public void LogFatal([InterpolatedStringHandlerArgument("")] ref LogInterpolatedStringHandler handler, [CallerArgumentExpression(nameof(handler))] string template = "")
    {
        if (handler.ShouldLog)
        {
            CaptureLog(SentryLogLevel.Fatal, handler.GetMessageAndClear(), SanitizeTemplate(template), handler.GetParametersAndClear(), null);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Fatal"/>, when enabled and sampled.
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    public void LogFatal(Action<SentryLog> configureLog, [InterpolatedStringHandlerArgument("")] ref LogInterpolatedStringHandler handler, [CallerArgumentExpression(nameof(handler))] string template = "")
    {
        if (handler.ShouldLog)
        {
            CaptureLog(SentryLogLevel.Fatal, handler.GetMessageAndClear(), SanitizeTemplate(template), handler.GetParametersAndClear(), configureLog);
        }
    }

    private static string SanitizeTemplate(string template)
    {
        var span = template.AsSpan();

        var start = span.IndexOf("$\"") + 2;
        var end = span.LastIndexOf("\"");
        var length = end - start;

        var sanitized = string.Create<(string template, int start, int length)>(length, (template, start, length), static (span, arg) =>
        {
            var source = arg.template.AsSpan().Slice(arg.start, arg.length);
            Debug.Assert(source.Length == span.Length);
            source.CopyTo(span);
        });
        return sanitized;
    }

    /// <summary>
    /// Provides an interpolated string handler for <see cref="SentryStructuredLogger" />,
    /// used by the language compiler to perform formatting for.
    /// </summary>
    /// <remarks>
    /// Intended for compiler-use only.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterpolatedStringHandler]
    public ref struct LogInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler _message;
        private ImmutableArray<KeyValuePair<string, object>>.Builder _parameters;
        private int _currentIndex = -1;

        /// <summary>
        /// Creates an instance of the handler used to translate an interpolated string into a Message and .
        /// </summary>
        /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
        /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
        /// <param name="condition">The condition Boolean passed to the <see cref="T:System.Diagnostics.Debug" /> method.</param>
        /// <param name="shouldAppend">A value indicating whether formatting should proceed.</param>
        /// <remarks>
        /// This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.
        /// </remarks>
        public LogInterpolatedStringHandler(int literalLength, int formattedCount, SentryStructuredLogger condition, out bool shouldAppend)
        {
            if (condition.GetType() == typeof(DefaultSentryStructuredLogger))
            {
                _message = new DefaultInterpolatedStringHandler(literalLength, formattedCount, CultureInfo.InvariantCulture);
                _parameters = ImmutableArray.CreateBuilder<KeyValuePair<string, object>>(formattedCount);
                shouldAppend = true;
            }
            else
            {
                _message = default;
                _parameters = null!;
                shouldAppend = false;
            }
        }

        internal bool ShouldLog => _parameters is not null;

        /// <summary>
        /// Writes the specified string to the handler.
        /// </summary>
        /// <param name="value">The string to write.</param>
        public void AppendLiteral(string value)
        {
            _message.AppendLiteral(value);
        }

        /// <summary>
        /// Writes the specified value to the handler.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public void AppendFormatted<T>(T value)
        {
            _message.AppendFormatted<T>(value);
            if (value is not null)
            {
                _parameters.Add(new KeyValuePair<string, object>((++_currentIndex).ToString(), value));
            }
        }

        /// <summary>
        /// Writes the specified value to the handler.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="format">The format string.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public void AppendFormatted<T>(T value, string? format)
        {
            _message.AppendFormatted<T>(value, format);
            if (value is not null)
            {
                _parameters.Add(new KeyValuePair<string, object>((++_currentIndex).ToString(), value));
            }
        }

        /// <summary>
        /// Writes the specified value to the handler.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public void AppendFormatted<T>(T value, int alignment)
        {
            _message.AppendFormatted<T>(value, alignment);
            if (value is not null)
            {
                _parameters.Add(new KeyValuePair<string, object>((++_currentIndex).ToString(), value));
            }
        }

        /// <summary>
        /// Writes the specified value to the handler.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <param name="format">The format string.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public void AppendFormatted<T>(T value, int alignment, string? format)
        {
            _message.AppendFormatted<T>(value, alignment, format);
            if (value is not null)
            {
                _parameters.Add(new KeyValuePair<string, object>((++_currentIndex).ToString(), value));
            }
        }

        /// <summary>
        /// Writes the specified character span to the handler.
        /// </summary>
        /// <param name="value">The span to write.</param>
        // public void AppendFormatted(scoped ReadOnlySpan<char> value)
        // {
        //     _message.AppendFormatted(value);
        //     if (value is not null)
        //     {
        //         _parameters.Add(new KeyValuePair<string, object>((++_currentIndex).ToString(), value));
        //     }
        // }

        /// <summary>
        /// Writes the specified string of chars to the handler.
        /// </summary>
        /// <param name="value">The span to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <param name="format">The format string.</param>
        // public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null)
        // {
        //     _message.AppendFormatted(value, alignment, format);
        //     if (value is not null)
        //     {
        //         _parameters.Add(new KeyValuePair<string, object>((++_currentIndex).ToString(), value));
        //     }
        //
        // }

        /// <summary>
        /// Writes the specified value to the handler.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void AppendFormatted(string? value)
        {
            _message.AppendFormatted(value);
            if (value is not null)
            {
                _parameters.Add(new KeyValuePair<string, object>((++_currentIndex).ToString(), value));
            }
        }

        /// <summary>
        /// Writes the specified value to the handler.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <param name="format">The format string.</param>
        public void AppendFormatted(string? value, int alignment = 0, string? format = null)
        {
            _message.AppendFormatted(value, alignment, format);
            if (value is not null)
            {
                _parameters.Add(new KeyValuePair<string, object>((++_currentIndex).ToString(), value));
            }
        }

        /// <summary>
        /// Writes the specified value to the handler.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <param name="format">The format string.</param>
        public void AppendFormatted(object? value, int alignment = 0, string? format = null)
        {
            _message.AppendFormatted(value, alignment, format);
            if (value is not null)
            {
                _parameters.Add(new KeyValuePair<string, object>((++_currentIndex).ToString(), value));
            }
        }

        /// <summary>
        /// Gets the built Message and clears the handler.
        /// </summary>
        /// <returns>The built string.</returns>
        internal string GetMessageAndClear()
        {
            return _message.ToStringAndClear();
        }

        /// <summary>
        /// Gets the built Parameters and clears the handler.
        /// </summary>
        /// <returns>The built string.</returns>
        internal ImmutableArray<KeyValuePair<string, object>> GetParametersAndClear()
        {
            var parameters = _parameters.DrainToImmutable();
            _parameters = null!;
            return parameters;
        }
    }
}
#endif
