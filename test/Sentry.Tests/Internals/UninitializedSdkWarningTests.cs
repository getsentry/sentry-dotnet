namespace Sentry.Tests.Internals;

public class UninitializedSdkWarningTests
{
    private const string Message = "Sentry is not initialized.";

    private readonly List<string> _integrationLog = [];
    private readonly List<string> _standardError = [];

    private UninitializedSdkWarning GetSut(string dsn = ValidDsn) =>
        new(_integrationLog.Add)
        {
            DsnLocator = () => dsn,
            WriteToStandardError = _standardError.Add
        };

    [Fact]
    public void WarnOnce_DsnFound_WritesToBothChannels()
    {
        GetSut().WarnOnce(Message);

        Assert.Equal(new[] { Message }, _integrationLog);
        Assert.Equal(new[] { Message }, _standardError);
    }

    [Fact]
    public void WarnOnce_NoDsnFound_WritesNothing()
    {
        GetSut(dsn: null).WarnOnce(Message);

        Assert.Empty(_integrationLog);
        Assert.Empty(_standardError);
    }

    [Fact]
    public void WarnOnce_CalledRepeatedly_WritesOnce()
    {
        var sut = GetSut();

        sut.WarnOnce(Message);
        sut.WarnOnce(Message);
        sut.WarnOnce(Message);

        Assert.Equal(new[] { Message }, _integrationLog);
        Assert.Equal(new[] { Message }, _standardError);
    }

    [Fact]
    public void WarnOnce_CalledConcurrently_WritesOnce()
    {
        var integrationWrites = 0;
        var standardErrorWrites = 0;
        var sut = new UninitializedSdkWarning(_ => Interlocked.Increment(ref integrationWrites))
        {
            DsnLocator = () => ValidDsn,
            WriteToStandardError = _ => Interlocked.Increment(ref standardErrorWrites)
        };

        Parallel.For(0, 64, _ => sut.WarnOnce(Message));

        Assert.Equal(1, integrationWrites);
        Assert.Equal(1, standardErrorWrites);
    }

    [Fact]
    public void WarnOnce_NoIntegrationLog_StillWritesToStandardError()
    {
        var sut = new UninitializedSdkWarning
        {
            DsnLocator = () => ValidDsn,
            WriteToStandardError = _standardError.Add
        };

        sut.WarnOnce(Message);

        Assert.Equal(new[] { Message }, _standardError);
    }

    [Fact]
    public void DsnLocator_ByDefault_ReadsTheEnvironment()
    {
        var variable = Constants.DsnEnvironmentVariable;
        var original = Environment.GetEnvironmentVariable(variable);
        try
        {
            Environment.SetEnvironmentVariable(variable, ValidDsn);
            Assert.Equal(ValidDsn, new UninitializedSdkWarning().DsnLocator());

            Environment.SetEnvironmentVariable(variable, SentryConstants.DisableSdkDsnValue);
            Assert.Null(new UninitializedSdkWarning().DsnLocator());
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, original);
        }
    }
}
