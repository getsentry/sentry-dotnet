using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Sentry Stacktrace interface.
/// </summary>
/// <remarks>
/// A stacktrace contains a list of frames, each with various bits (most optional) describing the context of that frame.
/// Frames should be sorted from oldest to newest.
/// </remarks>
/// <see href="https://develop.sentry.dev/sdk/event-payloads/stacktrace/"/>
public class SentryStackTrace : IJsonSerializable
{
    internal IList<SentryStackFrame>? InternalFrames { get; private set; }

    /// <summary>
    /// The list of frames in the stack.
    /// </summary>
    /// <remarks>
    /// The list of frames should be ordered by the oldest call first.
    /// </remarks>
    public IList<SentryStackFrame> Frames
    {
        get => InternalFrames ??= new List<SentryStackFrame>();
        set => InternalFrames = value;
    }

    /// <summary>
    /// The instruction address adjustment.
    /// </summary>
    /// <remarks>
    /// Tells the symbolicator if adjustment for the frame is needed.
    /// </remarks>
    public string? InstructionAddressAdjustment { get; set; }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteArrayIfNotEmpty("frames", InternalFrames, logger);
        writer.WriteStringIfNotWhiteSpace("instruction_addr_adjustment", InstructionAddressAdjustment);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryStackTrace FromJson(JsonElement json)
    {
        var frames = json
            .GetPropertyOrNull("frames")
            ?.EnumerateArray()
            .Select(SentryStackFrame.FromJson)
            .ToArray();

        return new SentryStackTrace
        {
            InternalFrames = frames
        };
    }
}
