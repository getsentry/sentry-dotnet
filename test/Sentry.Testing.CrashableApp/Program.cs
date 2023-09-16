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

        // Enable the SDK
        using var _ = SentrySdk.Init(options =>
        {
            options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
            options.Debug = true;
            options.IsGlobalModeEnabled = true;
        });

        Console.WriteLine($"Crashing with {crashType}");
        SentrySdk.CauseCrash(crashType);
    }
}
