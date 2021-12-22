using System.Web;

namespace Sentry.AspNet.Tests;
public class SentryHttpServerUtilityExtensionsTests
{
    [Fact]
    public void CaptureLastError_WithError_UnhandledErrorCaptured()
    {
        // Arrange
        var exception = new Exception();
        var id = SentryId.Create();
        var hub = Substitute.For<IHub>();
        hub.CaptureException(Arg.Any<Exception>()).Returns(id);
        var context = new HttpContext(new HttpRequest("", "http://test", null), new HttpResponse(new StringWriter()));
        context.AddError(exception);

        // Act
        var receivedId = context.Server.CaptureLastError(hub);

        // Assert
        hub.Received(1).CaptureException(Arg.Is(exception));
        Assert.False(exception.Data[Mechanism.HandledKey] as bool?);
        Assert.Equal("HttpApplication.Application_Error", exception.Data[Mechanism.MechanismKey]);
        Assert.Equal(id, receivedId);
    }

    [Fact]
    public void CaptureLastError_WithoutError_DoNothing()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var context = new HttpContext(new HttpRequest("", "http://test", null), new HttpResponse(new StringWriter()));

        // Act
        var receivedId = context.Server.CaptureLastError(hub);

        // Assert
        hub.Received(0).CaptureException(Arg.Any<Exception>());
        Assert.Equal(SentryId.Empty, receivedId);
    }

}
