using System;

namespace Sentry.PlatformAbstractions
{
    /// <summary>
    /// Details of the runtime
    /// </summary>
    /// <inheritdoc />
    public class Runtime : IEquatable<Runtime>
    {
        private static Runtime _runtime;
        /// <summary>
        /// Gets the current runtime
        /// </summary>
        /// <value>
        /// The current runtime.
        /// </value>
        public static Runtime Current => _runtime ?? (_runtime = RuntimeInfo.GetRuntime());
        /// <summary>
        /// The name of the runtime
        /// </summary>
        /// <example>
        /// .NET Framework, .NET Native, Mono
        /// </example>
        public string Name { get; internal set; }
        /// <summary>
        /// The version of the runtime
        /// </summary>
        /// <example>
        /// 4.7.2633.0
        /// </example>
        public string Version { get; internal set; }
        /// <summary>
        ///  A .NET Framework release key
        /// </summary>
        /// <remarks>
        /// Windows registry key:
        /// HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\Release
        /// Only applicable when on Windows, with full .NET Framework 4.5 and later.
        /// </remarks>
        /// <see href="https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed"/>
        public int? Release { get; internal set; }
        /// <summary>
        /// The raw value parsed to extract Name and Version
        /// </summary>
        /// <remarks>
        /// This property will contain a value when the underlying API
        /// returned Name and Version as a single string which required parsing.
        /// </remarks>
        public string Raw { get; internal set; }

        /// <summary>
        /// Creates a new Runtime instance
        /// </summary>
        /// <param name="name">The name of the runtime</param>
        /// <param name="version">The version of the runtime</param>
        /// <param name="release">The .NET Framework 4.5+ release number</param>
        /// <param name="raw">The raw value when parsing was required</param>
        public Runtime(
            string name = null,
            string version = null,
            int? release = null,
            string raw = null)
        {
            Name = name;
            Version = version;
            Release = release;
            Raw = raw;
        }

        /// <summary>
        /// The string representation of the Runtime
        /// </summary>
        public override string ToString()
        {
            if (Name == null && Version == null)
            {
                return Raw;
            }

            if (Name != null && Version == null)
            {
                return Raw?.Contains(Name) == true
                    ? Raw
                    : $"{Name} {Raw}";
            }

            return $"{Name} {Version}";
        }

        public bool Equals(Runtime other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(Name, other.Name)
                   && string.Equals(Version, other.Version)
                   && string.Equals(Raw, other.Raw) && Release == other.Release;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((Runtime)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Version != null ? Version.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Raw != null ? Raw.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Release.GetHashCode();
                return hashCode;
            }
        }
    }
}
