#pragma warning disable CS0618

namespace Sentry.Testing.CrashableApp;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello world 2");
        // Console.WriteLine($"Running CrashableApp with {args[0]}");
        // var crashType = (CrashType)Enum.Parse(typeof(CrashType), args[0]);
        // SentrySdk.CauseCrash(crashType);
    }
}
