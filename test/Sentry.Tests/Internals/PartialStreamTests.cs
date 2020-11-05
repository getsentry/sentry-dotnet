using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Sentry.Internal;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class PartialStreamTests
    {
        [Fact]
        public async Task PartialStream_WithOffsetAndLength_Length_ReturnsPartialLength()
        {
            // Arrange
            using var originalStream = new MemoryStream();
            await originalStream.FillWithRandomBytesAsync(1024);

            const int offset = 10;
            const int length = 100;
            using var partialStream = new PartialStream(originalStream, offset, length);

            // Act & assert
            partialStream.Length.Should().Be(length);
        }

        [Fact]
        public async Task PartialStream_WithOffsetAndLength_ReadToEnd_ReturnsOnlyDataInRange()
        {
            // Arrange
            using var originalStream = new MemoryStream();
            await originalStream.FillWithRandomBytesAsync(1024);

            const int offset = 10;
            const int length = 100;
            using var partialStream = new PartialStream(originalStream, offset, length);

            // Act
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
            using var originalStream = new MemoryStream();
            await originalStream.FillWithRandomBytesAsync(1024);

            const int offset = 10;
            using var partialStream = new PartialStream(originalStream, offset, null);

            // Act
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
            using var originalStream = new MemoryStream();
            await originalStream.FillWithRandomBytesAsync(1024);

            const int offset = 10;
            const int length = 100;
            using var partialStream = new PartialStream(originalStream, offset, length);

            // Act
            const int additionalOffset = 40;
            partialStream.Seek(additionalOffset, SeekOrigin.Begin);

            // Assert
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
            using var originalStream = new MemoryStream();
            await originalStream.FillWithRandomBytesAsync(1024);

            const int offset = 10;
            const int length = 100;
            using var partialStream = new PartialStream(originalStream, offset, length);

            // Act & assert
            Assert.Throws<InvalidOperationException>(() => partialStream.Position = 200);
        }

        [Fact]
        public async Task PartialStream_WithOffsetAndLength_InnerPositionChanged_StillReadsCorrectly()
        {
            // Arrange
            using var originalStream = new MemoryStream();
            await originalStream.FillWithRandomBytesAsync(1024);

            const int offset = 10;
            const int length = 100;
            using var partialStream = new PartialStream(originalStream, offset, length);

            // Act
            originalStream.Position = 1000;

            // Assert
            using var outputStream = new MemoryStream();
            await partialStream.CopyToAsync(outputStream);

            var originalPortion = originalStream.ToArray().Skip(offset).Take(length).ToArray();

            outputStream.ToArray().Should().Equal(originalPortion);
        }
    }
}
