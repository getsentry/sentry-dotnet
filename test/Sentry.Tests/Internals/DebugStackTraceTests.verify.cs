#nullable enable
// ReSharper disable once CheckNamespace
// Stack trace filters out Sentry frames by namespace

namespace Other.Tests.Internals;

public partial class DebugStackTraceTests
{
#if NET8_0_OR_GREATER
    // TODO: Create integration test to test this behaviour when publishing AOT apps
    // See https://github.com/getsentry/sentry-dotnet/issues/2772
    [Fact]
    public Task CreateFrame_ForNativeAOT()
    {
        var sut = _fixture.GetSut();
        var frame = sut.CreateFrame(new StubNativeAOTStackFrame()
        {
            Function = "DoSomething(int, long)",
            Module = "Foo.Bar",
            ImageBase = 1,
            IP = 2,
        });

        Assert.NotNull(frame);
        return VerifyJson(frame.ToJsonString());
    }
#endif
}
