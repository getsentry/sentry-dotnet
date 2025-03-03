namespace Sentry.Tests.Protocol;

public class UserFeedbackTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public UserFeedbackTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void Serialization_SentryUserFeedbacks_Success()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        // Arrange
        var eventId = new SentryId(Guid.Parse("acbe351c61494e7b807fd7e82a435ffc"));
        var userFeedback = new UserFeedback(eventId, "myName", "myEmail@service.com", "my comment");
        using var stream = new MemoryStream();

        // Act
        var actual = userFeedback.ToJsonString(_testOutputLogger, indented: true);

        // Assert
        Assert.Equal("""
            {
              "event_id": "acbe351c61494e7b807fd7e82a435ffc",
              "name": "myName",
              "email": "myEmail@service.com",
              "comments": "my comment"
            }
            """,
            actual);
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
