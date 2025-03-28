using System;

namespace ELFSharp.ELF
{
    internal static class Utilities
    {
        internal static T To<T>(this object source)
        {
            return (T)Convert.ChangeType(source, typeof(T));
        }
    }
}