using System.IO.Compression;

namespace Sentry.Tests.Internals;

public class GzipReadStreamTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1_000)]
    [InlineData(500_000)]
    public void Read_RoundTripsThroughGzip(int sourceLength)
    {
        // Arrange
        var source = new byte[sourceLength];
        for (var i = 0; i < source.Length; i++)
        {
            source[i] = (byte)('a' + i % 13);
        }

        using var stream = new GzipReadStream(new MemoryStream(source), CompressionLevel.Fastest);

        // Act
        using var compressed = new MemoryStream();
        stream.CopyTo(compressed);

        // Assert
        compressed.Position = 0;
        using var gunzip = new GZipStream(compressed, CompressionMode.Decompress);
        using var decompressed = new MemoryStream();
        gunzip.CopyTo(decompressed);
        decompressed.ToArray().Should().Equal(source);
    }

    [Fact]
    public void Read_SmallBuffer_ReturnsEveryByteOnce()
    {
        // Arrange
        var source = new byte[100_000];
        new Random(42).NextBytes(source);
        using var stream = new GzipReadStream(new MemoryStream(source), CompressionLevel.Optimal);

        // Act
        using var compressed = new MemoryStream();
        var buffer = new byte[7];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            compressed.Write(buffer, 0, read);
        }

        // Assert
        compressed.Position = 0;
        using var gunzip = new GZipStream(compressed, CompressionMode.Decompress);
        using var decompressed = new MemoryStream();
        gunzip.CopyTo(decompressed);
        decompressed.ToArray().Should().Equal(source);
    }

    [Fact]
    public void Read_AfterEnd_ReturnsZero()
    {
        // Arrange
        using var stream = new GzipReadStream(new MemoryStream(new byte[] { 1, 2, 3 }), CompressionLevel.Fastest);
        using var compressed = new MemoryStream();
        stream.CopyTo(compressed);

        // Act
        using var rest = new MemoryStream();
        stream.CopyTo(rest);

        // Assert
        rest.Length.Should().Be(0);
    }

    [Fact]
    public void Ctor_DoesNotReadSource()
    {
        // Arrange
        var source = Substitute.For<Stream>();
        source.CanRead.Returns(true);

        // Act
        using var _ = new GzipReadStream(source, CompressionLevel.Fastest);

        // Assert
        source.ReceivedCalls().Should().NotContain(call => call.GetMethodInfo().Name.StartsWith("Read"));
    }

    [Fact]
    public void Stream_IsReadOnlyAndNotSeekable()
    {
        // Arrange
        using var stream = new GzipReadStream(new MemoryStream(), CompressionLevel.Fastest);

        // Assert
        stream.CanRead.Should().BeTrue();
        stream.CanSeek.Should().BeFalse();
        stream.CanWrite.Should().BeFalse();
        stream.TryGetLength().Should().BeNull();
    }

    [Fact]
    public void Dispose_DisposesSource()
    {
        // Arrange
        var source = new MemoryStream(new byte[] { 1 });
        var stream = new GzipReadStream(source, CompressionLevel.Fastest);

        // Act
        stream.Dispose();

        // Assert
        source.CanRead.Should().BeFalse();
    }
}
