using Sentry.Extensibility;

namespace Sentry.Internal.DiagnosticSource;

/// <summary>
/// Various reflection helper methods.
/// </summary>
/// <remarks>
/// Note that the methods in this class are incompatible with Trimming. They are
/// used exclusively in the `Sentry.Internal.DiagnosticSource` integration, which
/// we disable if trimming is enabled to avoid problems at runtime.
/// </remarks>
internal static class ReflectionHelper
{
    [UnconditionalSuppressMessage("TrimAnalyzer", "IL2075: DynamicallyAccessedMembers", Justification = AotHelper.AvoidAtRuntime)]
    public static object? GetProperty(this object obj, string name, IDiagnosticLogger? logger = null)
    {
        if (AotHelper.IsTrimmed)
        {
            logger?.LogInfo("ReflectionHelper.GetProperty should not be used when trimming is enabled");
            return null;
        }

        var propertyNames = name.Split('.');
        var currentObj = obj;

        foreach (var propertyName in propertyNames)
        {
            var property = currentObj?.GetType().GetProperty(propertyName);
            if (property == null)
            {
                return null;
            }

            currentObj = property.GetValue(currentObj);
        }

        return currentObj;
    }

    public static Guid? GetGuidProperty(this object obj, string name, IDiagnosticLogger? logger = null) =>
        obj.GetProperty(name, logger) as Guid?;

    public static string? GetStringProperty(this object obj, string name, IDiagnosticLogger? logger = null) =>
        obj.GetProperty(name, logger) as string;
}
