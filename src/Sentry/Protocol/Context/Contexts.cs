using System.Collections.Concurrent;
using System.Runtime.Serialization;
using Sentry.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol
{
    /// <summary>
    /// Represents Sentry's structured Context
    /// </summary>
    /// <inheritdoc />
    /// <seealso href="https://docs.sentry.io/clientdev/interfaces/contexts/" />
    [DataContract]
    public class Contexts : ConcurrentDictionary<string, object>
    {
        /// <summary>
        /// Describes the application.
        /// </summary>
        public App App => this.GetOrCreate<App>(App.Type);
        /// <summary>
        /// Describes the browser.
        /// </summary>
        public Browser Browser => this.GetOrCreate<Browser>(Browser.Type);
        /// <summary>
        /// Describes the device.
        /// </summary>
        public Device Device => this.GetOrCreate<Device>(Device.Type);
        /// <summary>
        /// Defines the operating system.
        /// </summary>
        /// <remarks>
        /// In web contexts, this is the operating system of the browser (normally pulled from the User-Agent string).
        /// </remarks>
        public OperatingSystem OperatingSystem => this.GetOrCreate<OperatingSystem>(OperatingSystem.Type);
        /// <summary>
        /// This describes a runtime in more detail.
        /// </summary>
        public Runtime Runtime => this.GetOrCreate<Runtime>(Runtime.Type);

        /// <summary>
        /// Creates a deep clone of this context
        /// </summary>
        /// <returns></returns>
        internal Contexts Clone()
            => new Contexts
            {
                [App.Type] = (this[App.Type] as App)?.Clone(),
                [Browser.Type] = (this[Browser.Type] as Browser)?.Clone(),
                [Device.Type] = (this[Device.Type] as Device)?.Clone(),
                [OperatingSystem.Type] = (this[OperatingSystem.Type] as OperatingSystem)?.Clone(),
                [Runtime.Type] = (this[Runtime.Type] as Runtime)?.Clone()
            };
    }
}
