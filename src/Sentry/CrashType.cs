namespace Sentry;

/// <summary>
/// A type of application crash.
/// Used exclusively by <see cref="SentrySdk.CauseCrash"/>.
/// </summary>
[Obsolete("WARNING: This method deliberately causes a crash, and should not be used in a real application.")]
public enum CrashType
{
    /// <summary>
    /// A managed <see cref="ApplicationException"/> will be thrown from .NET.
    /// </summary>
    Managed,

    /// <summary>
    /// A managed <see cref="ApplicationException"/> will be thrown from .NET on a background thread.
    /// </summary>
    ManagedBackgroundThread,

#if ANDROID
        /// <summary>
        /// A <see cref="global::Java.Lang.RuntimeException"/> will be thrown from Java.
        /// </summary>
        Java,

        /// <summary>
        /// A <see cref="global::Java.Lang.RuntimeException"/> will be thrown from Java on a background thread.
        /// </summary>
        JavaBackgroundThread,
#endif

#if __MOBILE__ || NET8_0_OR_GREATER
    /// <summary>
    /// A native operation that will crash the application will be performed by a C library.
    /// </summary>
    Native
#endif

}
