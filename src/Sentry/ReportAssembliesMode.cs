namespace Sentry
{
    /// <summary>
    /// Possible modes for reporting the assemblies.
    /// </summary>
    public enum ReportAssembliesMode
    {
        /// <summary>
        /// Don't report any assemblies.
        /// </summary>
        None,

        /// <summary>
        /// Report assemblies and use the assembly version to determine the version.
        /// </summary>
        Version,

        /// <summary>
        /// Report assemblies and prefer the informational assembly version to determine the version. If
        /// the informational assembly version is not available, fall back to the assembly version.
        /// </summary>
        InformationalVersion
    }
}
