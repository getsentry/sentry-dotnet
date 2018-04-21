using System;

namespace Sentry.PlatformAbstractions
{
    /// <summary>
    /// Details of the runtime
    /// </summary>
    /// <inheritdoc />
    public class Runtime : IEquatable<Runtime>
    {
        /// <summary>
        /// The name of the runtime
        /// </summary>
        /// <example>
        /// .NET Framework, .NET Native, Mono
        /// </example>
        public string Name { get; }
        /// <summary>
        /// The version of the runtime
        /// </summary>
        /// <example>
        /// 4.7.2633.0
        /// </example>
        public string Version { get; }
        /// <summary>
        /// The raw value parsed to extract Name and Version
        /// </summary>
        /// <remarks>
        /// This property will contain a value when the underlying API
        /// returned Name and Version as a single string which required parsing.
        /// </remarks>
        public string Raw { get; }

        /// <summary>
        /// Creates a new Runtime instance
        /// </summary>
        /// <param name="name">The name of the runtime</param>
        /// <param name="version">The version of the runtime</param>
        /// <param name="raw">The raw value when parsing was required</param>
        public Runtime(string name, string version, string raw = null)
        {
            Name = name;
            Version = version;
            Raw = raw;
        }

        /// <summary>
        /// The string representation of the Runtime
        /// </summary>
        /// <returns>
        /// The raw value if any, or the Name and Version
        /// </returns>
        public override string ToString() => Raw ?? $"{Name} {Version}";

        public bool Equals(Runtime other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name)
                   && string.Equals(Version, other.Version)
                   && string.Equals(Raw, other.Raw);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Runtime) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Version != null ? Version.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Raw != null ? Raw.GetHashCode() : 0);
                return hashCode;
            }
        }

    }
}
