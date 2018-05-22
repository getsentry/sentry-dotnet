using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol
{
    /// <summary>
    /// Represents Sentry's structured Context
    /// </summary>
    /// <seealso href="https://docs.sentry.io/clientdev/interfaces/contexts/"/>
    [DataContract]
    public class Contexts
    {
        [DataMember(Name = "app", EmitDefaultValue = false)]
        private App _app;

        [DataMember(Name = "browser", EmitDefaultValue = false)]
        private Browser _browser;

        [DataMember(Name = "device", EmitDefaultValue = false)]
        private Device _device;

        [DataMember(Name = "os", EmitDefaultValue = false)]
        private OperatingSystem _operatingSystem;

        [DataMember(Name = "runtime", EmitDefaultValue = false)]
        private Runtime _runtime;

        /// <summary>
        /// Describes the application.
        /// </summary>
        public App App => _app ?? (_app = new App());
        /// <summary>
        /// Describes the browser.
        /// </summary>
        public Browser Browser => _browser ?? (_browser = new Browser());
        /// <summary>
        /// Describes the device.
        /// </summary>
        public Device Device => _device ?? (_device = new Device());
        /// <summary>
        /// Defines the operating system.
        /// </summary>
        /// <remarks>
        /// In web contexts, this is the operating system of the browser (normally pulled from the User-Agent string).
        /// </remarks>
        public OperatingSystem OperatingSystem => _operatingSystem ?? (_operatingSystem = new OperatingSystem());
        /// <summary>
        /// This describes a runtime in more detail.
        /// </summary>
        public Runtime Runtime => _runtime ?? (_runtime = new Runtime());

        /// <summary>
        /// Creates a deep clone of this context
        /// </summary>
        /// <returns></returns>
        internal Contexts Clone()
            => new Contexts
            {
                _app = _app?.Clone(),
                _browser = _browser?.Clone(),
                _device = _device?.Clone(),
                _operatingSystem = _operatingSystem?.Clone(),
                _runtime = _runtime?.Clone()
            };
    }
}
