#if !NET5_0_OR_GREATER
// ReSharper disable once CheckNamespace - It's a polyfill to a type on this location
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ModuleInitializerAttribute : Attribute
    {
    }
}
#endif
