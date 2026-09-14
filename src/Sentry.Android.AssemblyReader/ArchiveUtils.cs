namespace Sentry.Android.AssemblyReader;

internal static class ArchiveUtils
{
    internal static PEReader CreatePEReader(string assemblyName, MemoryStream inputStream, DebugLogger? logger)
    {
        var decompressedStream = TryDecompress(assemblyName, inputStream, logger); // Returns null if not compressed
        return new PEReader(decompressedStream ?? inputStream);
    }

    internal static MemoryStream Extract(this ZipArchiveEntry zipEntry)
    {
        var memStream = new MemoryStream((int)zipEntry.Length);
        using var zipStream = zipEntry.Open();
        zipStream.CopyTo(memStream);
        memStream.Position = 0;
        return memStream;
    }

    /// <summary>
    /// The DLL may be compressed, see https://github.com/xamarin/xamarin-android/pull/4686
    /// In particular: https://github.com/dotnet/android/blob/44c5c30d3da692c54ca27d4a41571ef20b73670f/src/Xamarin.Android.Build.Tasks/Utilities/AssemblyCompression.cs#L96-L104
    /// The format is:
    ///    [ 4 byte magic header ] (XALZ for LZ4, XAZS for Zstandard)
    ///    [ 4 byte descriptor header index ]
    ///    [ 4 byte uncompressed payload length ]
    ///    [rest: compressed payload]
    /// .NET 11 switched from LZ4 to Zstandard: https://github.com/dotnet/android/pull/11730
    /// </summary>
    /// <seealso href="https://github.com/dotnet/android/blob/f1aecf9e6ae80fe3f3992ec1f52ef953dac7c06b/.github/skills/read-assembly-store/src/AssemblyStore/AssemblyCompression.cs" />
    private static Stream? TryDecompress(string assemblyName, MemoryStream inputStream, DebugLogger? logger)
    {
        const uint lz4Magic = 0x5A4C4158; // 'XALZ', little-endian
        const uint zstandardMagic = 0x535A4158; // 'XAZS', little-endian
        const int payloadOffset = 12;
        var reader = new BinaryReader(inputStream);
        var magic = reader.ReadUInt32();
        if (magic is not (lz4Magic or zstandardMagic))
        {
            // Restore the input stream to the beginning if we're not decompressing.
            inputStream.Position = 0;
            return null;
        }
        reader.ReadUInt32(); // ignore descriptor index, we don't need it
        var decompressedLength = reader.ReadInt32();
        Debug.Assert(inputStream.Position == payloadOffset);
        var inputLength = (int)(inputStream.Length - payloadOffset);
        var format = magic == lz4Magic ? "LZ4" : "Zstandard";

        logger?.Invoke(DebugLoggerLevel.Debug, "Decompressing assembly ({0} bytes uncompressed) using {1}", decompressedLength, format);

        var outputStream = new MemoryStream(decompressedLength);

        // We're writing to the underlying array manually, so we need to set the length.
        outputStream.SetLength(decompressedLength);
        var outputBuffer = outputStream.GetBuffer();

        var inputBuffer = inputStream is MemorySlice slice ? slice.FullBuffer : inputStream.GetBuffer();
        var offset = inputStream is MemorySlice memorySlice ? memorySlice.Offset + payloadOffset : payloadOffset;
        var decoded = magic == lz4Magic
            ? LZ4Codec.Decode(inputBuffer, offset, inputLength, outputBuffer, 0, decompressedLength)
            : DecompressZstandard(assemblyName, inputBuffer.AsSpan(offset, inputLength), outputBuffer.AsSpan(0, decompressedLength));
        if (decoded != decompressedLength)
        {
            throw new Exception($"Failed to decompress {format} data of assembly {assemblyName} - decoded {decoded} instead of expected {decompressedLength} bytes");
        }
        return outputStream;
    }

#if NET11_0_OR_GREATER
    private static int DecompressZstandard(string assemblyName, ReadOnlySpan<byte> source, Span<byte> destination) =>
        ZstandardDecoder.TryDecompress(source, destination, out var bytesWritten) ? bytesWritten : -1;
#else
    private static int DecompressZstandard(string assemblyName, ReadOnlySpan<byte> source, Span<byte> destination) =>
        throw new NotSupportedException($"Assembly {assemblyName} is Zstandard compressed, which requires .NET 11 or later");
#endif

    // Allows consumer to access the underlying buffer even if the MemoryStream is created as a slice over another.
    // Plain MemoryStream would throw "MemoryStream's internal buffer cannot be accessed."
    internal class MemorySlice(MemoryStream other, int offset, int size)
        : MemoryStream(other.GetBuffer(), offset, size, writable: false)
    {
        public readonly int Offset = offset;
        public readonly byte[] FullBuffer = other.GetBuffer();
    }
}
