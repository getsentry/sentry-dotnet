using System.Reflection.PortableExecutable;

namespace Sentry.Android.AssemblyReader;

internal interface IAndroidAssemblyReader : IDisposable
{
    PEReader? TryReadAssembly(string name);
}
