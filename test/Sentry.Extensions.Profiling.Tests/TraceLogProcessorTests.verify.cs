using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;

namespace Sentry.Extensions.Profiling.Tests;

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

    [Fact]
    public Task ProfileWithTask()
    {
        var etlxFilePath = Path.Combine(_resourcesPath, "profile-with-task.etlx");
        using var eventLog = new TraceLog(etlxFilePath);
        var processor = new TraceLogProcessor(eventLog);
        var profile = processor.Process(CancellationToken.None);

        var json = profile.ToJsonString(_testOutputLogger);
        return VerifyJson(json);
    }
}
