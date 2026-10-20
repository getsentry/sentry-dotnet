using Sentry.Internal.Extensions;

namespace Sentry.Tests.Internals.Extensions;

public class StreamExtensionsTests
{
    private const int MaxLength = 1024;

    [Fact]
    public async Task ReadLineAsync_LineWithinLimit_ReadsIt()
    {
        // Arrange
        var line = new string('a', MaxLength);
        using var stream = (line + "\nrest").ToMemoryStream();

        // Act
        var result = await stream.ReadLineAsync(MaxLength);

        // Assert
        Encoding.UTF8.GetString(result).Should().Be(line);
    }

    [Fact]
    public async Task ReadLineAsync_NoNewlineWithinLimit_ThrowsWithoutReadingTheRest()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[16 * MaxLength]);

        // Act
        await Assert.ThrowsAsync<InvalidDataException>(async () => await stream.ReadLineAsync(MaxLength));

        // Assert
        stream.Position.Should().BeLessThan(2 * MaxLength);
    }

    [Fact]
    public async Task ReadLineAsync_NoMaxLength_ReadsToTheEnd()
    {
        // Arrange
        var line = new string('a', 16 * MaxLength);
        using var stream = line.ToMemoryStream();

        // Act
        var result = await stream.ReadLineAsync();

        // Assert
        Encoding.UTF8.GetString(result).Should().Be(line);
    }

    [Fact]
    public void TryGetLength_SeekableStream_ReturnsLength()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[42]);

        // Act
        var result = stream.TryGetLength();

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void TryGetLength_NonSeekableStream_ReturnsNull()
    {
        // Arrange
        using var stream = new LengthlessStream();

        // Act
        var result = stream.TryGetLength();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryGetLength_NonSeekableStreamReportingLength_ReturnsNull()
    {
        // Arrange
        using var stream = new NonSeekableMemoryStream(new byte[42]);

        // Act
        var result = stream.TryGetLength();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryGetLength_NullStream_ReturnsNull()
    {
        // Arrange
        Stream stream = null;

        // Act
        var result = stream.TryGetLength();

        // Assert
        result.Should().BeNull();
    }

    private class NonSeekableMemoryStream(byte[] buffer) : MemoryStream(buffer)
    {
        public override bool CanSeek => false;
    }
}
