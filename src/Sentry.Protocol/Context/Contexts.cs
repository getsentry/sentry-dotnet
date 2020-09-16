using System.Collections.Concurrent;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol
{
    /// <summary>
    /// Represents Sentry's structured Context.
    /// </summary>
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
        /// This describes a GPU of the device.
        /// </summary>
        public Gpu Gpu => this.GetOrCreate<Gpu>(Gpu.Type);

        /// <summary>
        /// Creates a deep clone of this context.
        /// </summary>
        internal Contexts Clone()
        {
            var context = new Contexts();

            CopyTo(context);

            return context;
        }

        /// <summary>
        /// Copies the items of the context while cloning the known types.
        /// </summary>
        internal void CopyTo(Contexts to)
        {
            foreach (var kv in this)
            {
                var value = kv.Key switch
                {
                    App.Type when kv.Value is App app => app.Clone(),
                    Browser.Type when kv.Value is Browser browser => browser.Clone(),
                    Device.Type when kv.Value is Device device => device.Clone(),
                    OperatingSystem.Type when kv.Value is OperatingSystem os => os.Clone(),
                    Runtime.Type when kv.Value is Runtime runtime => runtime.Clone(),
                    Gpu.Type when kv.Value is Gpu gpu => gpu.Clone(),
                    _ => kv.Value
                };

                to.TryAdd(kv.Key, value);
            }
        }
    }
}
