using FluentAssertions;
using Sentry.Internal;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Internals;

public class PartialStreamTests
{
    [Fact]
    public async Task PartialStream_WithOffsetAndLength_Length_ReturnsPartialLength()
    {
        // Arrange
#if !NET461 && !NETCOREAPP2_1
        await
#endif
            using var originalStream = new MemoryStream();
        await originalStream.FillWithRandomBytesAsync(1024);

        const int offset = 10;
        const int length = 100;
#if !NET461 && !NETCOREAPP2_1
        await
#endif
            using var partialStream = new PartialStream(originalStream, offset, length);

        // Act & assert
        partialStream.Length.Should().Be(length);
    }

    [Fact]
    public async Task PartialStream_WithOffsetAndLength_ReadToEnd_ReturnsOnlyDataInRange()
    {
        // Arrange
#if !NET461 && !NETCOREAPP2_1
        await
#endif
            using var originalStream = new MemoryStream();
        await originalStream.FillWithRandomBytesAsync(1024);

        const int offset = 10;
        const int length = 100;
#if !NET461 && !NETCOREAPP2_1
        await
#endif
            using var partialStream = new PartialStream(originalStream, offset, length);

        // Act
#if !NET461 && !NETCOREAPP2_1
        await
#endif
            using var outputStream = new MemoryStream();
        await partialStream.CopyToAsync(outputStream);

        // Assert
        var originalPortion = originalStream.ToArray().Skip(offset).Take(length).ToArray();

        outputStream.Length.Should().Be(length);
        outputStream.ToArray().Should().Equal(originalPortion);
    }

    [Fact]
    public async Task PartialStream_WithOffset_ReadToEnd_ReturnsOnlyDataInRange()
    {
        // Arrange
#if !NET461 && !NETCOREAPP2_1
        await
#endif
            using var originalStream = new MemoryStream();
        await originalStream.FillWithRandomBytesAsync(1024);

        const int offset = 10;
#if !NET461 && !NETCOREAPP2_1
        await
#endif
            using var partialStream = new PartialStream(originalStream, offset, null);

        // Act
#if !NET461 && !NETCOREAPP2_1
        await
#endif
            using var outputStream = new MemoryStream();
        await partialStream.CopyToAsync(outputStream);

        // Assert
        var originalPortion = originalStream.ToArray().Skip(offset).ToArray();

        outputStream.Length.Should().Be(originalStream.Length - offset);
        outputStream.ToArray().Should().Equal(originalPortion);
    }

    [Fact]
    public async Task PartialStream_WithOffsetAndLength_Seek_WorksCorrectly()
    {
        // Arrange
#if !NET461 && !NETCOREAPP2_1
        await
#endif
            using var originalStream = new MemoryStream();
        await originalStream.FillWithRandomBytesAsync(1024);

        const int offset = 10;
        const int length = 100;
#if !NET461 && !NETCOREAPP2_1
        await
#endif
            using var partialStream = new PartialStream(originalStream, offset, length);

        // Act
        const int additionalOffset = 40;
        partialStream.Seek(additionalOffset, SeekOrigin.Begin);

        // Assert
#if !NET461 && !NETCOREAPP2_1
        await
#endif
            using var outputStream = new MemoryStream();
        await partialStream.CopyToAsync(outputStream);

        var originalPortion = originalStream
            .ToArray()
            .Skip(offset + additionalOffset)
            .Take(length - additionalOffset)
            .ToArray();

        outputStream.ToArray().Should().Equal(originalPortion);
    }

    [Fact]
    public async Task PartialStream_WithOffsetAndLength_SettingInvalidPosition_Throws()
    {
        // Arrange
#if !NET461 && !NETCOREAPP2_1
        await
#endif
            using var originalStream = new MemoryStream();
        await originalStream.FillWithRandomBytesAsync(1024);

        const int offset = 10;
        const int length = 100;
#if !NET461 && !NETCOREAPP2_1
        await
#endif
            using var partialStream = new PartialStream(originalStream, offset, length);

        // Act & assert
        Assert.Throws<InvalidOperationException>(() => partialStream.Position = 200);
    }

    [Fact]
    public async Task PartialStream_WithOffsetAndLength_InnerPositionChanged_StillReadsCorrectly()
    {
        // Arrange
#if !NET461 && !NETCOREAPP2_1
        await
#endif
            using var originalStream = new MemoryStream();
        await originalStream.FillWithRandomBytesAsync(1024);

        const int offset = 10;
        const int length = 100;
#if !NET461 && !NETCOREAPP2_1
        await
#endif
            using var partialStream = new PartialStream(originalStream, offset, length);

        // Act
        originalStream.Position = 1000;

        // Assert
#if !NET461 && !NETCOREAPP2_1
        await
#endif
            using var outputStream = new MemoryStream();
        await partialStream.CopyToAsync(outputStream);

        var originalPortion = originalStream.ToArray().Skip(offset).Take(length).ToArray();

        outputStream.ToArray().Should().Equal(originalPortion);
    }
}
