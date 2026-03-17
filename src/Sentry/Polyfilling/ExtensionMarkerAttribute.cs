// ReSharper disable CheckNamespace
namespace System.Runtime.CompilerServices;

#if !NET10_0_OR_GREATER
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false)]
internal sealed class ExtensionMarkerAttribute : Attribute
{
    public ExtensionMarkerAttribute(string name)
        => Name = name;

    public string Name { get; }
}
#endif
