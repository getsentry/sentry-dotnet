using Sentry.Android;

// ReSharper disable once CheckNamespace
namespace Sentry;

public partial class SentryOptions
{
    /// <summary>
    /// The .NET SDK specific options for the Android platform.
    /// </summary>
    public AndroidOptions Android { get; }

    public class AndroidOptions
    {
        /// <summary>
        /// Gets or sets whether when LogCat logs are attached to events.
        /// The default is <see cref="LogCatIntegrationType.None"/>
        /// </summary>
        /// <seealso cref="LogCatMaxLines" />
        public LogCatIntegrationType LogCatIntegration { get; set; } = LogCatIntegrationType.None;

        /// <summary>
        /// Gets or sets the maximum number of lines to read from LogCat logs.
        /// The default value is 1000.
        /// </summary>
        /// <seealso cref="LogCatIntegration" />
        public int LogCatMaxLines { get; set; } = 1000;

        /// <summary>
        /// <para>
        /// Whether to suppress capturing SIGSEGV (Segfault) errors in the Native SDK.
        /// </para>
        /// <para>
        /// When managed code results in a NullReferenceException, this also causes a SIGSEGV (Segfault). Duplicate
        /// events (one managed and one native) can be prevented by suppressing native Segfaults, which may be
        /// convenient.
        /// </para>
        /// <para>
        /// Enabling this may prevent the capture of Segfault originating from native (not managed) code... so it may
        /// prevent the capture of genuine native Segfault errors.
        /// </para>
        /// </summary>
        public bool SuppressSegfaults { get; set; } = false;
    }
}
