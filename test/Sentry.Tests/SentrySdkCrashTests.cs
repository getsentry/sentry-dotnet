#if !__MOBILE__
#pragma warning disable CS0618

namespace Sentry.Tests;

public class SentrySdkCrashTests
{
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void CauseCrashInSeparateProcess(CrashType crashType)
    {
        RunCrashingApp(crashType, out var exitCode, out var stderr);

        Assert.Contains($"({nameof(CrashType)}.{crashType})", stderr);
        Assert.NotEqual(0, exitCode);
    }

    public static IEnumerable<object[]> GetTestCases =>
        Enum.GetValues(typeof(CrashType))
            .Cast<CrashType>()
            .Select(crashType => new object[] { crashType });

    private static void RunCrashingApp(CrashType crashType, out int exitCode, out string stderr)
    {
        var assembly = typeof(Testing.CrashableApp.Program).Assembly;

#if NETFRAMEWORK
        var filename = assembly.Location;
        var arguments = crashType.ToString();
#else
        const string filename = "dotnet";
        var arguments = $"{assembly.Location} {crashType}";
#endif
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = filename,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        });
        Assert.NotNull(process);

        process.WaitForExit();
        exitCode = process.ExitCode;
        stderr = process.StandardError.ReadToEnd();
    }
}
#endif
