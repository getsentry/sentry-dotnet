namespace Sentry.Internal;

internal static class ModuleExtensions
{
    /// <summary>
    /// The Module.Name for Modules that are embedded in SingleFileApps will be null
    /// or &lt;Unknown&gt;, in that case we can use Module.ScopeName instead
    /// </summary>
    /// <param name="module">A Module instance</param>
    /// <returns>module.Name, if this is available. module.ScopeName otherwise</returns>
    public static string? GetNameOrScopeName(this Module module){
        if (AotHelper.IsAot)
        {
            return module?.ScopeName;
        }

        return (module?.Name is null || module.Name.Equals("<Unknown>"))
            ? module?.ScopeName
            : module?.Name;
    }
}
