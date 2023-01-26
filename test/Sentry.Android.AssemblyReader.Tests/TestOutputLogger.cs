namespace Sentry.Android.AssemblyReader.Tests;

public class TestOutputLogger : IAndroidAssemblyReaderLogger
{
    private readonly ITestOutputHelper _outputHelper;

    public TestOutputLogger(ITestOutputHelper outputHelper) => _outputHelper = outputHelper;

    public void Log(string message, params object?[] args) => _outputHelper.WriteLine(message, args);
}
