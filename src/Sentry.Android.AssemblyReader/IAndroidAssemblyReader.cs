namespace Sentry.Android.AssemblyReader;

/// <summary>
/// Interface for an object that can read .NET assemblies from an Android APK.
/// </summary>
public interface IAndroidAssemblyReader : IDisposable
{
    /// <summary>
    /// Trys to get a <see cref="PEReader"/> for a given assembly.
    /// </summary>
    /// <param name="name">The name of the assembly.</param>
    /// <returns>The reader, or <c>null</c> if the assembly could not be found.</returns>
    public PEReader? TryReadAssembly(string name);
}
