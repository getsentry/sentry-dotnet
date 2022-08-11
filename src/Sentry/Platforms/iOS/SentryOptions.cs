// ReSharper disable once CheckNamespace
namespace Sentry;

public partial class SentryOptions
{
    /// <summary>
    /// Exposes additional options for the iOS platform.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public IosOptions iOS { get; } = new();

    /// <summary>
    /// Provides additional options for the iOS platform.
    /// </summary>
    public class IosOptions
    {
        internal IosOptions() { }

        // ---------- From Cocoa's SentryOptions.h ----------

        // TODO

        // ---------- Other ----------

        /// <summary>
        /// Gets or sets a value that indicates if tracing features are enabled on the embedded Cocoa SDK.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        public bool EnableCocoaSdkTracing { get; set; }

        // /// <summary>
        // /// Gets or sets a value that indicates if the <see cref="BeforeSend"/> callback will be invoked for
        // /// events that originate from the embedded Cocoa SDK. The default value is <c>false</c> (disabled).
        // /// </summary>
        // /// <remarks>
        // /// This is an experimental feature and is imperfect, as the .NET SDK and the embedded Cocoa SDK don't
        // /// implement all of the same features that may be present in the event graph. Some optional elements may
        // /// be stripped away during the roundtripping between the two SDKs.  Use with caution.
        // /// </remarks>
        // public bool EnableCocoaSdkBeforeSend { get; set; }
    }
}
