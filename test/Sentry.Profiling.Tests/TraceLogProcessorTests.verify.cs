using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.EventPipe;
using Microsoft.Diagnostics.Tracing.Parsers;

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
    //     var etlFilePath = "C:/dev/Aura.UI/profile.nettrace";
    //     var etlxFilePath = Path.ChangeExtension(etlFilePath, ".etlx");
    //     if (!File.Exists(etlxFilePath))
    //     {
    //         TraceLog.CreateFromEventTraceLogFile(etlFilePath, etlxFilePath);
    //     }
    //     using var eventLog = new TraceLog(etlxFilePath);
    //     var processor = new TraceLogProcessor(new(), eventLog);
    //     var profile = processor.Process(CancellationToken.None);
    //     var json = profile.ToJsonString(_testOutputLogger);
    // }

    private SampleProfile BuilProfile(TraceLogEventSource eventSource)
    {
        var builder = new SampleProfileBuilder(new() { DiagnosticLogger = _testOutputLogger }, eventSource.TraceLog);
        new SampleProfilerTraceEventParser(eventSource).ThreadSample += delegate (ClrThreadSampleTraceData data)
                {
                    builder.AddSample(data, data.TimeStampRelativeMSec);
                };
        eventSource.Process();
        return builder.Profile;
    }

    private SampleProfile GetProfile()
    {
        var etlxFilePath = Path.Combine(_resourcesPath, "sample.etlx");

        if (!File.Exists(etlxFilePath))
        {
            var etlFilePath = Path.ChangeExtension(etlxFilePath, "nettrace");
            var source = new EventPipeEventSource(etlFilePath);
            typeof(TraceLog)
            .GetMethod(
                "CreateFromEventPipeEventSources",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                new Type[] { typeof(TraceEventDispatcher), typeof(string), typeof(TraceLogOptions) })?
            .Invoke(null, new object[] { source, etlxFilePath, new TraceLogOptions() { ContinueOnError = true } });
        }

        using var traceLog = new TraceLog(etlxFilePath);
        using var eventSource = traceLog.Events.GetSource();
        return BuilProfile(eventSource);
    }

    [Fact]
    public Task Profile_Serialization_Works()
    {
        var json = GetProfile().ToJsonString(_testOutputLogger);
        return VerifyJson(json).DisableRequireUniquePrefix();
    }

    [Fact]
    public Task ProfileInfo_Serialization_Works()
    {
        var transaction = new SentryTransaction("name", "op");
        transaction.Contexts.Device.Architecture = "arch";
        transaction.Contexts.Device.Model = "device model";
        transaction.Contexts.Device.Manufacturer = "device make";
        transaction.Contexts.OperatingSystem.RawDescription = "Microsoft Windows 6.3.9600";
        var profile = GetProfile();
        var profileInfo = SamplingTransactionProfiler.CreateProfileInfo(transaction, profile);
        var json = profileInfo.ToJsonString(_testOutputLogger);
        return VerifyJson(json);
    }
}
