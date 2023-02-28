namespace Sentry.Tests.Protocol;

[UsesVerify]
public partial class ProfilerTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public ProfilerTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    private void AddStack(SampleProfile sut, List<int> frames)
    {
        var stack = new HashableGrowableArray<int>();
        foreach (var frame in frames)
        {
            stack.Add(frame);
        }
        stack.Seal();
        sut.Stacks.Add(stack);
    }

    [Fact]
    public Task SampleProfile_Serialization()
    {
        var sut = new SampleProfile();
        sut.Samples.Add(new()
        {
            StackId = 4,
            ThreadId = 5,
            Timestamp = 6
        });
        sut.Samples.Add(new()
        {
            StackId = 1,
            ThreadId = 2,
            Timestamp = 3
        });

        sut.Frames.Add(new()
        {
            Function = "Frame0"
        });
        sut.Frames.Add(new()
        {
            Function = "Frame1"
        });
        sut.Frames.Add(new()
        {
            Function = "Frame2"
        });


        AddStack(sut, new() { 0, 1, 2 });
        AddStack(sut, new() { 2, 2, 0 });
        AddStack(sut, new() { 1, 0, 2 });

        sut.Threads[1] = new()
        {
            Name = "Thread 1"
        };
        sut.Threads[5] = new()
        {
            Name = "Thread 5"
        };

        var json = sut.ToJsonString(_testOutputLogger);
        return VerifyJson(json);
    }
}
