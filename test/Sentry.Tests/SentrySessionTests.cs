namespace Sentry.Tests;

public class SentrySessionTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public SentrySessionTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void Serialization_Session_Success()
    {
        // Arrange
        var session = new SentrySession(
            SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"),
            "bar",
            DateTimeOffset.Parse("2020-01-01T00:00:00+00:00", CultureInfo.InvariantCulture),
            "release123",
            "env123",
            "192.168.0.1",
            "Google Chrome");

        session.ReportError();
        session.ReportError();
        session.ReportError();

        var sessionUpdate = new SessionUpdate(
            session,
            true,
            DateTimeOffset.Parse("2020-01-02T00:00:00+00:00", CultureInfo.InvariantCulture),
            5,
            SessionEndStatus.Crashed);

        // Act
        var json = sessionUpdate.ToJsonString(_testOutputLogger, indented: true);

        // Assert
        json.Should().Be("""
            {
              "sid": "75302ac48a024bde9a3b3734a82e36c8",
              "did": "bar",
              "init": true,
              "started": "2020-01-01T00:00:00+00:00",
              "timestamp": "2020-01-02T00:00:00+00:00",
              "seq": 5,
              "duration": 86400,
              "errors": 3,
              "status": "crashed",
              "attrs": {
                "release": "release123",
                "environment": "env123",
                "ip_address": "192.168.0.1",
                "user_agent": "Google Chrome"
              }
            }
            """);
    }

    [Fact]
    public void CreateUpdate_IncrementsSequenceNumber()
    {
        // Arrange
        var session = new SentrySession(
            SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"),
            "bar",
            DateTimeOffset.Parse("2020-01-01T00:00:00+00:00", CultureInfo.InvariantCulture),
            "release123",
            "env123",
            "192.168.0.1",
            "Google Chrome");

        // Act
        var sessionUpdate1 = session.CreateUpdate(true, DateTimeOffset.Now);
        var sessionUpdate2 = session.CreateUpdate(false, DateTimeOffset.Now);
        var sessionUpdate3 = session.CreateUpdate(false, DateTimeOffset.Now);

        // Assert
        sessionUpdate1.SequenceNumber.Should().Be(0);
        sessionUpdate2.SequenceNumber.Should().Be(1);
        sessionUpdate3.SequenceNumber.Should().Be(2);
    }
}
