namespace Sentry;

/// <summary>
/// Native session replay options.
/// </summary>
public class NativeSentryReplayOptions
{
    /// <summary>
    /// Gets or sets the percentage of a session
    /// replay to be sent. The value should be between 0.0
    /// and 1.0. Default is 0.
    /// </summary>
    /// <remarks>
    /// <see href="https://docs.sentry.io/product/explore/session-replay/mobile/">sentry.io</see>
    /// </remarks>
    public float SessionSampleRate { get; set; }

    /// <summary>
    /// Gets or sets the percentage of an error
    /// replay to be sent. The value should be between 0.0
    /// and 1.0. Default is 0.
    /// </summary>
    /// <remarks>
    /// <see href="https://docs.sentry.io/product/explore/session-replay/mobile/">sentry.io</see>
    /// </remarks>
    public float OnErrorSampleRate { get; set; }

    /// <summary>
    /// Gets or sets a value determining whether
    /// text should be masked during replays.
    /// </summary>
    /// <remarks>
    /// <see href="https://docs.sentry.io/product/explore/session-replay/mobile/">sentry.io</see>
    /// </remarks>
    public bool MaskAllText { get; set; } = true;

    /// <summary>
    /// Gets or sets a value determining whether
    /// images should be masked during replays.
    /// </summary>
    /// <remarks>
    /// <see href="https://docs.sentry.io/product/explore/session-replay/mobile/">sentry.io</see>
    /// </remarks>
    public bool MaskAllImages { get; set; } = true;

    /// <summary>
    /// Gets or sets the quality of session replays.
    /// Higher quality means finer details
    /// but a greater performance impact.
    /// Default is <see cref="SentryReplayQuality.Medium"/>.
    /// </summary>
    /// <remarks>
    /// <see href="https://docs.sentry.io/platforms/android/session-replay/performance-overhead/">Android</see>
    /// <see href="https://docs.sentry.io/platforms/apple/guides/ios/session-replay/performance-overhead/">iOS</see>
    /// </remarks>
    public SentryReplayQuality Quality { get; set; } = SentryReplayQuality.Medium;
}
