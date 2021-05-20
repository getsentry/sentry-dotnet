namespace Sentry
{
    /// <summary>
    /// Terminal state of a session.
    /// </summary>
    public enum SessionEndState
    {
        /// <summary>
        /// Session ended normally.
        /// </summary>
        Exited,

        /// <summary>
        /// Session ended with an error.
        /// </summary>
        Crashed,

        /// <summary>
        /// Session ended abnormally (e.g. device lost power).
        /// </summary>
        Abnormal
    }
}
