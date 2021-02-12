#if !NET5_0
// ReSharper disable once CheckNamespace - It's a polyfill to a type on this location
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ModuleInitializerAttribute : Attribute
    {
    }
}
#endif
