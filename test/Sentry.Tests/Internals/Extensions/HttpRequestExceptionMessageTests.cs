namespace Sentry.Tests.Internals.Extensions;

#if !NET5_0_OR_GREATER
public class HttpRequestExceptionMessageTests
{
    [Fact]
    public void EnsureSuccessStatusCode_StatusCodeInRange_DoesNotThrow()
    {
        // Arrange
        const HttpStatusCode statusCode = HttpStatusCode.OK; // Any status code in the 200-299 range

        // Act
        var act = () => statusCode.EnsureSuccessStatusCode();

        // Assert
        act.Should().NotThrow<HttpRequestException>();
    }

    [Fact]
    public void EnsureSuccessStatusCode_StatusCodeOutOfRange_ThrowsHttpRequestException()
    {
        var unsuccessfulStatusCodes = new List<int>();
        unsuccessfulStatusCodes.AddRange(Enumerable.Range(0, 199));
        unsuccessfulStatusCodes.AddRange(Enumerable.Range(300, 600));
        foreach (var i in unsuccessfulStatusCodes)
        {
            // Arrange and Act
            var statusCode = (HttpStatusCode)i;
            var act = () => statusCode.EnsureSuccessStatusCode();

            // Assert
            act.Should().Throw<HttpRequestException>()
                .WithMessage(string.Format(
                    CultureInfo.InvariantCulture,
                    "Response status code does not indicate success: {0}",
                    (int)statusCode
                ));
        }
    }
}
#endif
