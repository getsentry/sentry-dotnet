using System.Reflection.Metadata;
using K4os.Compression.LZ4;

namespace Sentry.Android.AssemblyReader.Tests;

// Assembly.Location is empty on Android, where assemblies load from the APK. The device run covers
// decompression through AndroidAssemblyReaderTests.ReadsAssembly instead.
#if !ANDROID
public class ArchiveUtilsTests
{
    private const uint Lz4Magic = 0x5A4C4158; // 'XALZ'
    private const uint ZstandardMagic = 0x535A4158; // 'XAZS'

    private static readonly byte[] Assembly = File.ReadAllBytes(typeof(ArchiveUtilsTests).Assembly.Location);

    [Fact]
    public void CreatePEReader_Uncompressed_ReadsAssembly()
    {
        using var peReader = ArchiveUtils.CreatePEReader("test.dll", new MemoryStream(Assembly), null);

        AssertIsThisAssembly(peReader);
    }

    [Fact]
    public void CreatePEReader_Lz4_ReadsAssembly()
    {
        var compressed = new byte[LZ4Codec.MaximumOutputSize(Assembly.Length)];
        var length = LZ4Codec.Encode(Assembly, 0, Assembly.Length, compressed, 0, compressed.Length);

        using var peReader = ArchiveUtils.CreatePEReader("test.dll", WithHeader(Lz4Magic, compressed.AsSpan(0, length)), null);

        AssertIsThisAssembly(peReader);
    }

#if NET11_0_OR_GREATER
    [Fact]
    public void CreatePEReader_Zstandard_ReadsAssembly()
    {
        var compressed = new byte[ZstandardEncoder.GetMaxCompressedLength(Assembly.Length)];
        ZstandardEncoder.TryCompress(Assembly, compressed, out var length).Should().BeTrue();

        using var peReader = ArchiveUtils.CreatePEReader("test.dll", WithHeader(ZstandardMagic, compressed.AsSpan(0, length)), null);

        AssertIsThisAssembly(peReader);
    }

    [Fact]
    public void CreatePEReader_CorruptZstandard_Throws()
    {
        var garbage = new byte[64];

        var act = () => ArchiveUtils.CreatePEReader("test.dll", WithHeader(ZstandardMagic, garbage), null);

        act.Should().Throw<Exception>().WithMessage("*Zstandard*test.dll*");
    }
#else
    [Fact]
    public void CreatePEReader_Zstandard_ThrowsNotSupported()
    {
        var act = () => ArchiveUtils.CreatePEReader("test.dll", WithHeader(ZstandardMagic, new byte[64]), null);

        act.Should().Throw<NotSupportedException>().WithMessage("*test.dll*Zstandard*");
    }
#endif

    [Fact]
    public void CreatePEReader_SliceOfLargerBuffer_ReadsAssembly()
    {
        var compressed = new byte[LZ4Codec.MaximumOutputSize(Assembly.Length)];
        var length = LZ4Codec.Encode(Assembly, 0, Assembly.Length, compressed, 0, compressed.Length);
        var entry = WithHeader(Lz4Magic, compressed.AsSpan(0, length)).ToArray();

        const int prefix = 100;
        var store = new MemoryStream();
        store.Write(new byte[prefix]);
        store.Write(entry);
        var slice = new ArchiveUtils.MemorySlice(store, prefix, entry.Length);

        using var peReader = ArchiveUtils.CreatePEReader("test.dll", slice, null);

        AssertIsThisAssembly(peReader);
    }

    private static MemoryStream WithHeader(uint magic, ReadOnlySpan<byte> payload)
    {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(magic);
            writer.Write(0u); // descriptor index
            writer.Write(Assembly.Length);
            writer.Write(payload);
        }
        stream.Position = 0;
        return stream;
    }

    private static void AssertIsThisAssembly(PEReader peReader)
    {
        peReader.HasMetadata.Should().BeTrue();
        peReader.GetMetadataReader().GetAssemblyDefinition().GetAssemblyName().Name
            .Should().Be(typeof(ArchiveUtilsTests).Assembly.GetName().Name);
    }
}
#endif
