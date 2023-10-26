namespace Sentry.Tests.Protocol;

[UsesVerify]
[UniqueForAot]
public class ProfilerTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public ProfilerTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    private static void AddStack(SampleProfile sut, List<int> frames)
    {
        var stack = new Internal.GrowableArray<int>(frames.Count);
        foreach (var frame in frames)
        {
            stack.Add(frame);
        }
        sut.Stacks.Add(stack);
    }

    private static SampleProfile CreateSampleProfile()
    {
        var prof = new SampleProfile();

        prof.Samples.Add(new()
        {
            StackId = 4,
            ThreadId = 1,
            Timestamp = 6
        });
        prof.Samples.Add(new()
        {
            StackId = 1,
            ThreadId = 0,
            Timestamp = 3
        });

        prof.Frames.Add(new()
        {
            Function = "Frame0"
        });
        prof.Frames.Add(new()
        {
            Function = "Frame1"
        });
        prof.Frames.Add(new()
        {
            Function = "Frame2"
        });


        AddStack(prof, new() { 0, 1, 2 });
        AddStack(prof, new() { 2, 2, 0 });
        AddStack(prof, new() { 1, 0, 2 });

        prof.Threads.Add(new()
        {
            Name = "Thread 1"
        });
        prof.Threads.Add(new()
        {
            Name = "Thread 5"
        });

        return prof;
    }

    [Fact]
    public Task SampleProfile_Serialization()
    {
        var sut = CreateSampleProfile();
        var json = sut.ToJsonString(_testOutputLogger);
        return VerifyJson(json);
    }

    [Fact]
    public Task ProfileInfo_Serialization()
    {
        var sut = new ProfileInfo();
        sut.StartTimestamp = DateTimeOffset.UtcNow;
        sut.DebugMeta.Images = new List<DebugImage> {
            new () {
                ImageAddress = 5
            }
        };
        sut.Profile = CreateSampleProfile();
        sut.Environment = "env name";
        sut.Release = "1.0 (123)";
        sut.Transaction = new("tx name", "tx operation");
        sut.Contexts.Device.Architecture = "arch";
        sut.Contexts.Device.Model = "device model";
        sut.Contexts.Device.Manufacturer = "device make";
        sut.Contexts.OperatingSystem.RawDescription = "Microsoft Windows 6.3.9600";

        var json = sut.ToJsonString(_testOutputLogger);
        return VerifyJson(json);
    }

    [Fact]
    public Task ProfileInfo_WithoutDebugImages_Serialization()
    {
        var sut = new ProfileInfo();
        sut.StartTimestamp = DateTimeOffset.UtcNow;
        sut.Profile = CreateSampleProfile();
        sut.Environment = "env name";
        sut.Release = "1.0 (123)";
        sut.Transaction = new("tx name", "tx operation");
        sut.Contexts.Device.Architecture = "arch";
        sut.Contexts.Device.Model = "device model";
        sut.Contexts.Device.Manufacturer = "device make";
        sut.Contexts.OperatingSystem.RawDescription = "Microsoft Windows 6.3.9600";

        var json = sut.ToJsonString(_testOutputLogger);
        return VerifyJson(json);
    }
}
