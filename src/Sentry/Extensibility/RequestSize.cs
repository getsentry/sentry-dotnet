namespace Sentry.Extensibility
{
    /// <summary>
    /// The size allowed when extracting a request body in a web application.
    /// </summary>
    public enum RequestSize
    {
        /// <summary>
        /// No request payload is extracted
        /// </summary>
        /// <remarks>This is the default value. Opt-in is required.</remarks>
        None,
        /// <summary>
        /// A small payload is extracted.
        /// </summary>
        Small,
        /// <summary>
        /// A medium payload is extracted.
        /// </summary>
        Medium,
        /// <summary>
        /// A large payload is extracted.
        /// </summary>
        Large
    }
}
