using Sentry.Internal.Extensions;
using SentryStreamExtensions = Sentry.Internal.Extensions.StreamExtensions;

namespace Sentry.Tests.Internals.Extensions;

public class StreamExtensionsTests
{
    [Fact]
    public async Task ReadLineAsync_LineWithinLimit_ReadsIt()
    {
        // Arrange
        var line = new string('a', SentryStreamExtensions.MaxLineLength);
        using var stream = (line + "\nrest").ToMemoryStream();

        // Act
        var result = await stream.ReadLineAsync();

        // Assert
        Encoding.UTF8.GetString(result).Should().Be(line);
    }

    [Fact]
    public async Task ReadLineAsync_NoNewlineWithinLimit_ThrowsWithoutReadingTheRest()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[16 * SentryStreamExtensions.MaxLineLength]);

        // Act
        await Assert.ThrowsAsync<InvalidDataException>(async () => await stream.ReadLineAsync());

        // Assert
        stream.Position.Should().BeLessThan(2 * SentryStreamExtensions.MaxLineLength);
    }
}
