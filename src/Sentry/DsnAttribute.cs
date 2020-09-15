using System;

namespace Sentry
{
    /// <summary>
    /// A way to configure the DSN via attribute defined at the entry-assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class DsnAttribute : Attribute
    {
        /// <summary>
        /// The string DSN or empty string to turn the SDK off.
        /// </summary>
        public string Dsn { get; }

        /// <summary>
        /// Creates a new instance of <see cref="T:Sentry.DsnAttribute" />.
        /// </summary>
        public DsnAttribute(string dsn) => Dsn = dsn;
    }
}

