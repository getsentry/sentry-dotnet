using Sentry.AspNet;

public class SentryHttpServerUtilityExtensionsTests
{
    private class Fixture
    {
        public SentryId Id { get; set; }

        public IHub GetSut()
        {
            Id = SentryId.Create();
            var hub = Substitute.For<IHub>();
            hub.IsEnabled.Returns(true);
            hub.CaptureEvent(Arg.Any<SentryEvent>()).Returns(Id);
            return hub;
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void CaptureLastError_WithError_UnhandledErrorCaptured()
    {
        // Arrange
        var hub = _fixture.GetSut();
        var exception = new Exception();

        var context = HttpContextBuilder.Build();
        context.AddError(exception);

        // Act
        var receivedId = context.Server.CaptureLastError(hub);

        // Assert
        hub.Received(1).CaptureEvent(Arg.Is<SentryEvent>(@event => @event.Exception == exception));
        Assert.False(exception.Data[Mechanism.HandledKey] as bool?);
        Assert.Equal("HttpApplication.Application_Error", exception.Data[Mechanism.MechanismKey]);
        Assert.Equal(_fixture.Id, receivedId);
    }

    [Fact]
    public void CaptureLastError_WithoutError_DoNothing()
    {
        // Arrange
        var hub = _fixture.GetSut();
        var context = HttpContextBuilder.Build();

        // Act
        var receivedId = context.Server.CaptureLastError(hub);

        // Assert
        hub.Received(0).CaptureEvent(Arg.Any<SentryEvent>());
        Assert.Equal(SentryId.Empty, receivedId);
    }
}
