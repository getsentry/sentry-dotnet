namespace Sentry;

/// <summary>
/// Builds a span while recording a transaction that has already completed elsewhere (for example, work
/// measured on another machine and replayed through a proxy). Unlike <see cref="ISpan"/>, timing is supplied
/// up-front rather than measured live, so a recorded span can never be left half-specified.
/// </summary>
/// <remarks>
/// Obtained from <see cref="ITransactionRecorder"/> or a parent <see cref="ISpanRecorder"/> via
/// <see cref="RecordSpan(string, DateTimeOffset, TimeSpan, SpanId?, Action{ISpanRecorder}?)"/>.
/// See <see cref="HubExtensions.RecordTransaction"/>.
/// </remarks>
public interface ISpanRecorder
{
    /// <summary>
    /// The span's id. When not overridden at creation, one is generated.
    /// </summary>
    SpanId SpanId { get; }

    /// <summary>
    /// Span description.
    /// </summary>
    string? Description { get; set; }

    /// <summary>
    /// Span status. Defaults to <see cref="SpanStatus.Ok"/> when not set.
    /// </summary>
    SpanStatus? Status { get; set; }

    /// <summary>
    /// Sets a tag on the span.
    /// </summary>
    void SetTag(string key, string value);

    /// <summary>
    /// Sets arbitrary data on the span.
    /// </summary>
    void SetData(string key, object? value);

    /// <summary>
    /// Records a child span nested under this one. The parent is structural (this span), so no parent id
    /// needs to be supplied. Pass <paramref name="spanId"/> to preserve an id from the originating system.
    /// </summary>
    /// <param name="operation">The span operation.</param>
    /// <param name="startTimestamp">When the child span started.</param>
    /// <param name="duration">How long the child span ran. Must not be negative.</param>
    /// <param name="spanId">Optional span id to preserve; generated when omitted.</param>
    /// <param name="configure">Optional callback to set metadata and record further nested spans.</param>
    /// <returns>The recorder for the child span.</returns>
    ISpanRecorder RecordSpan(
        string operation,
        DateTimeOffset startTimestamp,
        TimeSpan duration,
        SpanId? spanId = null,
        Action<ISpanRecorder>? configure = null);
}

/// <summary>
/// Builds a transaction (and its span tree) that has already completed elsewhere. Obtained inside the
/// <c>configure</c> callback of <see cref="HubExtensions.RecordTransaction"/>. When the callback returns, the
/// whole tree is materialized and captured once — no live tracing, stopwatch, or sampling roll is involved.
/// </summary>
public interface ITransactionRecorder : ISpanRecorder
{
    /// <summary>
    /// The trace this transaction belongs to. Set at creation (via <see cref="HubExtensions.RecordTransaction"/>)
    /// and inherited by every recorded span.
    /// </summary>
    SentryId TraceId { get; }

    /// <summary>
    /// The release that produced this transaction. Useful when the origin system differs from this process.
    /// </summary>
    string? Release { get; set; }

    /// <summary>
    /// The environment the transaction ran in. Useful when the origin system differs from this process.
    /// </summary>
    string? Environment { get; set; }
}
