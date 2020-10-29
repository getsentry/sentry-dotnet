#if DEBUG // The event Id is defined by the client.
// Inspecting the response id is used for debugging only
namespace Sentry.Internal.Http
{
    /// <summary>
    /// The payload of a response to a successful call to Sentry.
    /// </summary>
    /// <seealso href="https://develop.sentry.dev/sdk/overview/#reading-the-response"/>
    // ReSharper disable All
    internal class SentrySuccessfulResponseBody
    {
        /// <summary>
        /// The id generated for the event or the one created by the SDK if any.
        /// </summary>
        /// <example>
        /// fc6d8c0c43fc4630ad850ee518f1b9d0
        /// </example>
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S3459 // Unassigned members should be removed
        public string? id { get; set; }
    }
}
#endif
