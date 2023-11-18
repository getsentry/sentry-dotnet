namespace Sentry.Testing.CrashableApp;

public static class Program
{
    public static void Main(string[] args)
    {
#pragma warning disable CS0618
        var crashType = (CrashType)Enum.Parse(typeof(CrashType), args[0]);
        SentrySdk.CauseCrash(crashType);
#pragma warning restore CS0618
    }
}
