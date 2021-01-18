namespace Sentry
{
    /// <summary>
    /// The mode which the SDK builds the stack trace.
    /// </summary>
    /// <remarks>
    /// Changing this WILL affect issue grouping in Sentry since the format of the frames will change.
    /// </remarks>
    public enum StackTraceMode
    {
        /// <summary>
        /// The default .NET stack trace format.
        /// </summary>
        /// <remarks>
        /// This was the default before Sentry .NET 3.0.0.
        /// </remarks>
        Original,
        /// <summary>
        /// Includes return type, arguments ref modifiers and more.
        /// </summary>
        /// <remarks>
        /// This mode uses <see href="https://github.com/getsentry/Ben.Demystifier">Ben Adams' Demystifier library</see>.
        /// </remarks>
        Enhanced,
    }
}
