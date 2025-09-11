using Sentry.AspNet.Internal;

namespace Sentry.AspNet.Tests.Internal;

public class SystemWebRequestEventProcessorTests :
    HttpContextTest
{
    private class Fixture
    {
        public IRequestPayloadExtractor RequestPayloadExtractor { get; set; } = Substitute.For<IRequestPayloadExtractor>();
        public SentryOptions SentryOptions { get; set; } = new();
        public object MockBody { get; set; } = new();

        public Fixture()
        {
            _ = RequestPayloadExtractor.ExtractPayload(Arg.Any<IHttpRequest>()).Returns(MockBody);
        }

        public SystemWebRequestEventProcessor GetSut()
        {
            return new SystemWebRequestEventProcessor(RequestPayloadExtractor, SentryOptions);
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Ctor_NullEvent_ThrowsArgumentNullException()
    {
        _fixture.RequestPayloadExtractor = null;
        _ = Assert.Throws<ArgumentNullException>(() => _fixture.GetSut());
    }

    [Fact]
    public void Process_NullEvent_ReturnsNull() => Assert.Null(_fixture.GetSut().Process(null));

    [Fact]
    public void Process_DefaultFixture_ReadsMockBody()
    {
        var expected = new SentryEvent();

        var sut = _fixture.GetSut();

        var actual = sut.Process(expected);
        Assert.Same(expected, actual);
        Assert.Same(_fixture.MockBody, expected.Request.Data);
    }

    [Fact]
    public void Process_NoHttpContext_NoRequestData()
    {
        Context = null;
        var expected = new SentryEvent();

        var sut = _fixture.GetSut();

        var actual = sut.Process(expected);
        Assert.Same(expected, actual);
    }

    [Fact]
    public void Process_NoBodyExtracted_NoRequestData()
    {
        _ = _fixture.RequestPayloadExtractor.ExtractPayload(Arg.Any<IHttpRequest>()).Returns(null);
        var expected = new SentryEvent();

        var sut = _fixture.GetSut();

        var actual = sut.Process(expected);
        Assert.Same(expected, actual);
        Assert.Null(expected.Request.Data);
    }

    [Fact]
    public void Process_PresetUserIP_NotOverwritten()
    {
        const string userIp = "192.0.0.1";
        var evt = new SentryEvent();
        evt.User.IpAddress = userIp;

        _fixture.SentryOptions.SendDefaultPii = true;
        var sut = _fixture.GetSut();

        var processedEvt = sut.Process(evt);
        Assert.Equal(userIp, processedEvt?.User.IpAddress);
    }
}
