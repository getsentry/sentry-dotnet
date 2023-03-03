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

    [Fact]
    public Task ProfileWithTask()
    {
        var etlxFilePath = Path.Combine(_resourcesPath, "profile-with-task.etlx");
        using var eventLog = new TraceLog(etlxFilePath);
        var processor = new TraceLogProcessor(eventLog);
        var profile = processor.Process();

        var json = profile.ToJsonString(_testOutputLogger);
        return VerifyJson(json);
    }
}
