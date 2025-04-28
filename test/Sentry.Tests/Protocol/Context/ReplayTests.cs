namespace Sentry.Tests.Protocol.Context;

public class ReplayTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public ReplayTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void Ctor_NoPropertyFilled_SerializesEmptyObject()
    {
        // Arrange
        var replay = new Replay();

        // Act
        var actual = replay.ToJsonString(_testOutputLogger);

        // Assert
        Assert.Equal("""{"type":"replay"}""", actual);
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        // Arrange
        var replay = new Replay
        {
            ReplayId = SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8")
        };

        // Act
        var actual = replay.ToJsonString(_testOutputLogger, indented: true);

        // Assert
        Assert.Equal(
            """
            {
              "type": "replay",
              "replay_id": "75302ac48a024bde9a3b3734a82e36c8"
            }
            """,
            actual);
    }

    [Fact]
    public void Clone_CopyValues()
    {
        // Arrange
        var replay = new Replay
        {
            ReplayId = SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8")
        };

        // Act
        var clone = replay.Clone();

        // Assert
        Assert.Equal(replay.ReplayId, clone.ReplayId);
    }
}
