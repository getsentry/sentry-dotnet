using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Extension methods for <see cref="ISentryClient"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryClientExtensions
{
    /// <summary>
    /// Captures the exception.
    /// </summary>
    /// <param name="client">The Sentry client.</param>
    /// <param name="ex">The exception.</param>
    /// <returns>The Id of the event</returns>
    public static SentryId CaptureException(this ISentryClient client, Exception ex) =>
        client.IsEnabled ? client.CaptureEvent(new SentryEvent(ex)) : SentryId.Empty;

    /// <summary>
    /// Captures a message.
    /// </summary>
    /// <param name="client">The Sentry client.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="level">The message level.</param>
    /// <returns>The Id of the event</returns>
    public static SentryId CaptureMessage(this ISentryClient client, string message,
        SentryLevel level = SentryLevel.Info)
    {
        if (client.IsEnabled && !string.IsNullOrWhiteSpace(message))
        {
            return client.CaptureEvent(new SentryEvent
            {
                Message = message,
                Level = level
            });
        }

        return SentryId.Empty;
    }

    /// <summary>
    /// Captures a user feedback.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="eventId">The event Id.</param>
    /// <param name="email">The user email.</param>
    /// <param name="comments">The user comments.</param>
    /// <param name="name">The optional username.</param>
    public static void CaptureUserFeedback(this ISentryClient client, SentryId eventId, string email, string comments,
        string? name = null)
    {
        if (!client.IsEnabled)
        {
            return;
        }

        client.CaptureUserFeedback(new UserFeedback(eventId, name, email, comments));
    }

    /// <summary>
    /// Flushes the queue of captured events until the timeout set in <see cref="SentryOptions.FlushTimeout"/>
    /// is reached.
    /// </summary>
    /// <param name="client">The Sentry client.</param>
    /// <remarks>
    /// Blocks synchronously. Prefer <see cref="FlushAsync"/> in async code.
    /// </remarks>
    public static void Flush(this ISentryClient client) =>
        client.FlushAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Flushes the queue of captured events until the timeout is reached.
    /// </summary>
    /// <param name="client">The Sentry client.</param>
    /// <param name="timeout">The amount of time allowed for flushing.</param>
    /// <remarks>
    /// Blocks synchronously. Prefer <see cref="ISentryClient.FlushAsync(TimeSpan)"/> in async code.
    /// </remarks>
    public static void Flush(this ISentryClient client, TimeSpan timeout) =>
        client.FlushAsync(timeout).GetAwaiter().GetResult();

    /// <summary>
    /// Flushes the queue of captured events until the timeout set in <see cref="SentryOptions.FlushTimeout"/>
    /// is reached.
    /// </summary>
    /// <param name="client">The Sentry client.</param>
    /// <returns>A task to await for the flush operation.</returns>
    public static Task FlushAsync(this ISentryClient client)
    {
        var options = client.GetSentryOptions() ?? new SentryOptions();
        var timeout = options.FlushTimeout;
        return client.FlushAsync(timeout);
    }

    internal static SentryOptions? SentryOptionsForTestingOnly { get; set; }

    internal static SentryOptions? GetSentryOptions(this ISentryClient clientOrHub) =>
        clientOrHub switch
        {
            SentryClient client => client.Options,
            Hub hub => hub.Options,
            HubAdapter => SentrySdk.CurrentOptions,
            _ => SentryOptionsForTestingOnly
        };
}
