using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;

namespace Sentry.Profiling.Tests;

[UsesVerify]
public class TraceLogProcessorTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public TraceLogProcessorTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    private readonly string _resourcesPath = Path.GetFullPath(Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", "..", "..", "Resources"));

    // This is not a test case but rather an entrypoint for manual debugging with a known `.nettrace` file.
    // [Fact]
    // public void ManualDebugging()
    // {
    //     var etlFilePath = "C:/dev/dotnet/temp/f806155815dd40969608324788cf371b.nettrace";
    //     var etlxFilePath = Path.ChangeExtension(etlFilePath, ".etlx");
    //     if (!File.Exists(etlxFilePath))
    //     {
    //         TraceLog.CreateFromEventTraceLogFile(etlFilePath, etlxFilePath);
    //     }
    //     using var eventLog = new TraceLog(etlxFilePath);
    //     var processor = new TraceLogProcessor(eventLog);
    //     var profile = processor.Process();
    //     var json = profile.ToJsonString(_testOutputLogger);
    // }

    private SampleProfile GetProfile()
    {
        var etlxFilePath = Path.Combine(_resourcesPath, "profile-with-task.etlx");

        // Code to update the ETLX (just for backup so we know how it came to be:)
        // var etlFilePath = Path.Combine(_resourcesPath, "profile-with-task.nettrace");
        // var source = new EventPipeEventSource(etlFilePath);
        // new Downsampler().AttachTo(source);
        // typeof(TraceLog)
        // .GetMethod(
        //     "CreateFromEventPipeEventSources",
        //     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
        //     new Type[] { typeof(TraceEventDispatcher), typeof(string), typeof(TraceLogOptions) })?
        // .Invoke(null, new object[] { source, etlxFilePath, new TraceLogOptions() { ContinueOnError = true } });

        using var eventLog = new TraceLog(etlxFilePath);
        var processor = new TraceLogProcessor(eventLog);
        return processor.Process(CancellationToken.None);
    }

    [Fact]
    public Task ProfileWithTask()
    {
        var profile = GetProfile();
        var json = profile.ToJsonString(_testOutputLogger);
        return VerifyJson(json);
    }

    [Fact]
    public Task ProfileInfoWithTask()
    {
        var transaction = new Transaction("name", "op");
        transaction.Contexts.Device.Architecture = "arch";
        transaction.Contexts.Device.Model = "device model";
        transaction.Contexts.Device.Manufacturer = "device make";
        transaction.Contexts.OperatingSystem.RawDescription = "Microsoft Windows 6.3.9600";
        var profile = GetProfile();
        var profileInfo = SamplingTransactionProfiler.CreateProfileInfo(transaction, DateTimeOffset.UtcNow, profile);
        var json = profileInfo.ToJsonString(_testOutputLogger);
        return VerifyJson(json);
    }
}
