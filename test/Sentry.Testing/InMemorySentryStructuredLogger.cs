#nullable enable

namespace Sentry.Testing;

public sealed class InMemorySentryStructuredLogger : SentryStructuredLogger
{
    public List<LogEntry> Entries { get; } = new();
    public List<SentryLog> Logs { get; } = new();

    /// <inheritdoc />
    private protected override void CaptureLog(SentryLogLevel level, string template, object[]? parameters, Action<SentryLog>? configureLog)
    {
        Entries.Add(LogEntry.Create(level, template, parameters));
    }

    /// <inheritdoc />
    protected internal override void CaptureLog(SentryLog log)
    {
        Logs.Add(log);
    }

    /// <inheritdoc />
    protected internal override void Flush()
    {
        // no-op
    }

    public sealed class LogEntry : IEquatable<LogEntry>
    {
        public static LogEntry Create(SentryLogLevel level, string template, object[]? parameters)
        {
            return new LogEntry(level, template, parameters is null ? ImmutableArray<object>.Empty : ImmutableCollectionsMarshal.AsImmutableArray(parameters));
        }

        private LogEntry(SentryLogLevel level, string template, ImmutableArray<object> parameters)
        {
            Level = level;
            Template = template;
            Parameters = parameters;
        }

        public SentryLogLevel Level { get; }
        public string Template { get; }
        public ImmutableArray<object> Parameters { get; }

        public void AssertEqual(SentryLogLevel level, string template, params object[] parameters)
        {
            var expected = Create(level, template, parameters);
            Assert.Equal(expected, this);
        }

        public bool Equals(LogEntry? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Level == other.Level
                && Template == other.Template
                && Parameters.SequenceEqual(other.Parameters);
        }

        public override bool Equals(object? obj)
        {
            return obj is LogEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            throw new UnreachableException();
        }
    }
}
