namespace Sentry.PlatformAbstractions;

/// <summary>
/// Details of the runtime
/// </summary>
public class Runtime : IEquatable<Runtime>
{
    /// <summary>
    /// Gets the current runtime
    /// </summary>
    /// <value>
    /// The current runtime.
    /// </value>
    public static Runtime Current { get; } = RuntimeInfo.GetRuntime();

    /// <summary>
    /// The name of the runtime
    /// </summary>
    /// <example>
    /// .NET Framework, .NET Native, Mono
    /// </example>
    public string? Name { get; }

    /// <summary>
    /// The version of the runtime
    /// </summary>
    /// <example>
    /// 4.7.2633.0
    /// </example>
    public string? Version { get; }

#if NETFRAMEWORK
        /// <summary>
        /// The .NET Framework installation which is running the process
        /// </summary>
        /// <value>
        /// The framework installation or null if not running .NET Framework
        /// </value>
        public FrameworkInstallation? FrameworkInstallation { get; }
#endif

    /// <summary>
    /// The raw value parsed to extract Name and Version
    /// </summary>
    /// <remarks>
    /// This property will contain a value when the underlying API
    /// returned Name and Version as a single string which required parsing.
    /// </remarks>
    public string? Raw { get; }

    /// <summary>
    /// The .NET Runtime Identifier of the runtime
    /// </summary>
    /// <remarks>
    /// This property will be populated for .NET 5 and newer, or <c>null</c> otherwise.
    /// </remarks>
    public string? Identifier { get; }

    /// <summary>
    /// Creates a new Runtime instance
    /// </summary>
#if NETFRAMEWORK
        public Runtime(
            string? name = null,
            string? version = null,
            FrameworkInstallation? frameworkInstallation = null,
            string? raw = null)
        {
            Name = name;
            Version = version;
            FrameworkInstallation = frameworkInstallation;
            Raw = raw;
            Identifier = null;
        }
#else
    public Runtime(
        string? name = null,
        string? version = null,
        string? raw = null,
        string? identifier = null)
    {
        Name = name;
        Version = version;
        Raw = raw;
        Identifier = identifier;
    }
#endif

    /// <summary>
    /// The string representation of the Runtime
    /// </summary>
    public override string? ToString()
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

    /// <summary>
    /// Compare instances for equality.
    /// </summary>
    /// <param name="other">The instance to compare against.</param>
    /// <returns>True if the instances are equal by reference or its state.</returns>
    public bool Equals(Runtime? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(Name, other.Name)
               && string.Equals(Version, other.Version)
               && string.Equals(Raw, other.Raw)
#if NETFRAMEWORK
                   && Equals(FrameworkInstallation, other.FrameworkInstallation);
#else
               && Equals(Identifier, other.Identifier);
#endif
    }

    /// <summary>
    /// Compare instances for equality.
    /// </summary>
    /// <param name="obj">The instance to compare against.</param>
    /// <returns>True if the instances are equal by reference or its state.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((Runtime)obj);
    }

    /// <summary>
    /// Get the hashcode of this instance.
    /// </summary>
    /// <returns>The hashcode of the instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Name?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ (Version?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ (Raw?.GetHashCode() ?? 0);
#if NETFRAMEWORK
                hashCode = (hashCode * 397) ^ (FrameworkInstallation?.GetHashCode() ?? 0);
#else
            hashCode = (hashCode * 397) ^ (Identifier?.GetHashCode() ?? 0);
#endif
            return hashCode;
        }
    }
}
