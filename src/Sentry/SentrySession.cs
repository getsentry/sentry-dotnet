namespace Sentry;

/// <summary>
/// Sentry session.
/// </summary>
// https://develop.sentry.dev/sdk/sessions
public class SentrySession : ISentrySession
{
    /// <inheritdoc />
    public SentryId Id { get; }

    /// <inheritdoc />
    public string? DistinctId { get; }

    /// <inheritdoc />
    public DateTimeOffset StartTimestamp { get; }

    /// <inheritdoc />
    public string Release { get; }

    /// <inheritdoc />
    public string? Environment { get; }

    /// <inheritdoc />
    public string? IpAddress { get; }

    /// <inheritdoc />
    public string? UserAgent { get; }

    private int _errorCount;

    /// <inheritdoc />
    // Sentry's implementation changed to only care whether it's 0 or 1 (has error(s))
    public int ErrorCount => _errorCount;

    // Start at -1 so that the first increment puts it at 0
    private int _sequenceNumber = -1;

    internal SentrySession(
        SentryId id,
        string? distinctId,
        DateTimeOffset startTimestamp,
        string release,
        string? environment,
        string? ipAddress,
        string? userAgent)
    {
        Id = id;
        DistinctId = distinctId;
        StartTimestamp = startTimestamp;
        Release = release;
        Environment = environment;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SentrySession"/>.
    /// </summary>
    public SentrySession(string? distinctId, string release, string? environment)
        : this(
            SentryId.Create(),
            distinctId,
            DateTimeOffset.Now,
            release,
            environment,
            null,
            null)
    {
    }

    /// <summary>
    /// Reports an error on the session.
    /// </summary>
    public void ReportError() => Interlocked.Increment(ref _errorCount);

    internal SessionUpdate CreateUpdate(
        bool isInitial,
        DateTimeOffset timestamp,
        SessionEndStatus? endStatus = null) =>
        new(this, isInitial, timestamp, Interlocked.Increment(ref _sequenceNumber), endStatus);
}
