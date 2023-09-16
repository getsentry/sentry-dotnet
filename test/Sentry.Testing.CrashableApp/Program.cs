#pragma warning disable CS0618

namespace Sentry.Testing.CrashableApp;

public static class Program
{
    public static void Main(string[] args)
    {
#if MACOS
        Console.WriteLine("Running on a Mac target");
#endif
        Console.WriteLine($"Running CrashableApp with {args[0]}");
        var crashType = (CrashType)Enum.Parse(typeof(CrashType), args[0]);
        Console.WriteLine($"Crashing with {crashType}");
        SentrySdk.CauseCrash(crashType);
    }
}
