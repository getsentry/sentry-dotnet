namespace Sentry.Internal;

internal static class ModuleExtensions
{
    /// <summary>
    /// The Module.Name for Modules that are embedded in SingleFileApps will be null
    /// or &lt;Unknown&gt;, in that case we can use Module.ScopeName instead
    /// </summary>
    /// <param name="module">A Module instance</param>
    /// <returns>module.Name, if this is available. module.ScopeName otherwise</returns>
    [UnconditionalSuppressMessage("SingleFile", "IL3002:Avoid calling members marked with 'RequiresAssemblyFilesAttribute' when publishing as a single-file", Justification = AotHelper.SuppressionJustification)]
    public static string? GetNameOrScopeName(this Module module)
    {
        return (AotHelper.IsNativeAot || module?.Name is null || module.Name.Equals("<Unknown>"))
            ? module?.ScopeName
            : module?.Name;
    }
}
