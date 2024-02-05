using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Session update.
/// </summary>
// https://develop.sentry.dev/sdk/sessions/#session-update-payload
public class SessionUpdate : ISentrySession, IJsonSerializable
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

    /// <inheritdoc />
    public int ErrorCount { get; }

    /// <summary>
    /// Whether this is the initial update.
    /// </summary>
    public bool IsInitial { get; }

    /// <summary>
    /// Timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Sequence number.
    /// </summary>
    public int SequenceNumber { get; }

    /// <summary>
    /// Duration of time since the start of the session.
    /// </summary>
    public TimeSpan Duration => Timestamp - StartTimestamp;

    /// <summary>
    /// Status with which the session was ended.
    /// </summary>
    public SessionEndStatus? EndStatus { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SessionUpdate"/>.
    /// </summary>
    public SessionUpdate(
        SentryId id,
        string? distinctId,
        DateTimeOffset startTimestamp,
        string release,
        string? environment,
        string? ipAddress,
        string? userAgent,
        int errorCount,
        bool isInitial,
        DateTimeOffset timestamp,
        int sequenceNumber,
        SessionEndStatus? endStatus)
    {
        Id = id;
        DistinctId = distinctId;
        StartTimestamp = startTimestamp;
        Release = release;
        Environment = environment;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        ErrorCount = errorCount;
        IsInitial = isInitial;
        Timestamp = timestamp;
        SequenceNumber = sequenceNumber;
        EndStatus = endStatus;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SessionUpdate"/>.
    /// </summary>
    public SessionUpdate(
        ISentrySession session,
        bool isInitial,
        DateTimeOffset timestamp,
        int sequenceNumber,
        SessionEndStatus? endStatus)
        : this(
            session.Id,
            session.DistinctId,
            session.StartTimestamp,
            session.Release,
            session.Environment,
            session.IpAddress,
            session.UserAgent,
            session.ErrorCount,
            isInitial,
            timestamp,
            sequenceNumber,
            endStatus)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SessionUpdate"/>.
    /// </summary>
    public SessionUpdate(SessionUpdate sessionUpdate, bool isInitial, SessionEndStatus? endStatus)
        : this(
            sessionUpdate,
            isInitial,
            sessionUpdate.Timestamp,
            sessionUpdate.SequenceNumber,
            endStatus)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SessionUpdate"/>.
    /// </summary>
    public SessionUpdate(SessionUpdate sessionUpdate, bool isInitial)
        : this(sessionUpdate, isInitial, sessionUpdate.EndStatus)
    {
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteSerializable("sid", Id, logger);
        writer.WriteStringIfNotWhiteSpace("did", DistinctId);
        writer.WriteBoolean("init", IsInitial);
        writer.WriteString("started", StartTimestamp);
        writer.WriteString("timestamp", Timestamp);
        writer.WriteNumber("seq", SequenceNumber);
        writer.WriteNumber("duration", (int)Duration.TotalSeconds);
        writer.WriteNumber("errors", ErrorCount);
        writer.WriteStringIfNotWhiteSpace("status", EndStatus?.ToString().ToSnakeCase());

        // Attributes
        writer.WriteStartObject("attrs");
        writer.WriteString("release", Release);
        writer.WriteStringIfNotWhiteSpace("environment", Environment);
        writer.WriteStringIfNotWhiteSpace("ip_address", IpAddress);
        writer.WriteStringIfNotWhiteSpace("user_agent", UserAgent);
        writer.WriteEndObject();

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses <see cref="SessionUpdate"/> from JSON.
    /// </summary>
    public static SessionUpdate FromJson(JsonElement json)
    {
        var id = json.GetProperty("sid").GetStringOrThrow().Pipe(SentryId.Parse);
        var distinctId = json.GetPropertyOrNull("did")?.GetString();
        var startTimestamp = json.GetProperty("started").GetDateTimeOffset();
        var release = json.GetProperty("attrs").GetProperty("release").GetStringOrThrow();
        var environment = json.GetProperty("attrs").GetPropertyOrNull("environment")?.GetString();
        var ipAddress = json.GetProperty("attrs").GetPropertyOrNull("ip_address")?.GetString();
        var userAgent = json.GetProperty("attrs").GetPropertyOrNull("user_agent")?.GetString();
        var errorCount = json.GetPropertyOrNull("errors")?.GetInt32() ?? 0;
        var isInitial = json.GetPropertyOrNull("init")?.GetBoolean() ?? false;
        var timestamp = json.GetProperty("timestamp").GetDateTimeOffset();
        var sequenceNumber = json.GetProperty("seq").GetInt32();
        var endStatus = json.GetPropertyOrNull("status")?.GetString()?.ParseEnum<SessionEndStatus>();

        return new SessionUpdate(
            id,
            distinctId,
            startTimestamp,
            release,
            environment,
            ipAddress,
            userAgent,
            errorCount,
            isInitial,
            timestamp,
            sequenceNumber,
            endStatus);
    }
}
