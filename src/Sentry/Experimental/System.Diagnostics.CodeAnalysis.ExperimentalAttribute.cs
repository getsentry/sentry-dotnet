#if !NET8_0_OR_GREATER
// ReSharper disable CheckNamespace
// ReSharper disable ConvertToPrimaryConstructor
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Assembly |
                AttributeTargets.Module |
                AttributeTargets.Class |
                AttributeTargets.Struct |
                AttributeTargets.Enum |
                AttributeTargets.Constructor |
                AttributeTargets.Method |
                AttributeTargets.Property |
                AttributeTargets.Field |
                AttributeTargets.Event |
                AttributeTargets.Interface |
                AttributeTargets.Delegate, Inherited = false)]
internal sealed class ExperimentalAttribute : Attribute
{
    public ExperimentalAttribute(string diagnosticId)
    {
        DiagnosticId = diagnosticId;
    }

    public string DiagnosticId { get; }

    public string? UrlFormat { get; set; }
}
#endif
