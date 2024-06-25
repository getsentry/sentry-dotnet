namespace Sentry.Tests.Protocol;

public class UserTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public UserTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        var sut = new SentryUser
        {
            Id = "user-id",
            Username = "user-name",
            Email = "test@sentry.io",
            IpAddress = "::1",
#pragma warning disable CS0618 // Type or member is obsolete
            Segment = "A1",
#pragma warning restore CS0618 // Type or member is obsolete
            Other = new Dictionary<string, string> { { "testCustomValueKey", "testCustomValue" } }
        };

        var actual = sut.ToJsonString(_testOutputLogger, indented: true);

        Assert.Equal("""
            {
              "id": "user-id",
              "username": "user-name",
              "email": "test@sentry.io",
              "ip_address": "::1",
              "segment": "A1",
              "other": {
                "testCustomValueKey": "testCustomValue"
              }
            }
            """,
            actual);
    }

    [Fact]
    public void Clone_CopyValues()
    {
        var sut = new SentryUser
        {
            Id = "id",
            Email = "emal@sentry.io",
            IpAddress = "::1",
            Username = "user",
#pragma warning disable CS0618 // Type or member is obsolete
            Segment = "segment",
#pragma warning restore CS0618 // Type or member is obsolete
            Other = new Dictionary<string, string>
            {
                {"testCustomValueKey", "testCustomValue"}
            }
        };

        var clone = sut.Clone();

        Assert.Equal(sut.Id, clone.Id);
        Assert.Equal(sut.Username, clone.Username);
        Assert.Equal(sut.Email, clone.Email);
        Assert.Equal(sut.IpAddress, clone.IpAddress);
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Equal(sut.Segment, clone.Segment);
#pragma warning restore CS0618 // Type or member is obsolete
        Assert.Equal(sut.Other, clone.Other);
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializeObject_TestCase_SerializesAsExpected((SentryUser user, string serialized) @case)
    {
        var actual = @case.user.ToJsonString(_testOutputLogger);
        Assert.Equal(@case.serialized, actual);
    }

    public static IEnumerable<object[]> TestCases()
    {
        yield return new object[] { (new SentryUser(), "{}") };
        yield return new object[] { (new SentryUser { Id = "some id" }, """{"id":"some id"}""") };
        yield return new object[] { (new SentryUser { Username = "some username" }, """{"username":"some username"}""") };
        yield return new object[] { (new SentryUser { Email = "some email" }, """{"email":"some email"}""") };
        yield return new object[] { (new SentryUser { IpAddress = "some ipAddress" }, """{"ip_address":"some ipAddress"}""") };
#pragma warning disable CS0618 // Type or member is obsolete
        yield return new object[] { (new SentryUser { Segment = "some segment" }, """{"segment":"some segment"}""") };
#pragma warning restore CS0618 // Type or member is obsolete

        var other = new Dictionary<string, string> { { "testCustomValueKey", "testCustomValue" } };
        yield return new object[] { (new SentryUser { Other = other }, """{"other":{"testCustomValueKey":"testCustomValue"}}""") };
    }
}
