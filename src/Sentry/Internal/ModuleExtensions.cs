namespace Sentry.Internal;

internal static class ModuleExtensions
{
    // See https://learn.microsoft.com/en-us/dotnet/api/system.reflection.module.fullyqualifiedname?view=net-8.0#remarks
    internal const string UnknownLocation = "<Unknown>";

    /// <summary>
    /// The Module.Name for Modules that are embedded in SingleFileApps will be null
    /// or &lt;Unknown&gt;, in that case we can use Module.ScopeName instead
    /// </summary>
    /// <param name="module">A Module instance</param>
    /// <returns>module.Name, if this is available. module.ScopeName otherwise</returns>
    [UnconditionalSuppressMessage("SingleFile", "IL3002: calling members marked with 'RequiresAssemblyFilesAttribute'", Justification = AotHelper.AvoidAtRuntime)]
    public static string? GetNameOrScopeName(this Module module)
    {
        return (AotHelper.IsTrimmed || module?.Name is null || module.Name.Equals(UnknownLocation))
            ? module?.ScopeName
            : module?.Name;
    }
}
