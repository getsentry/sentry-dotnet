namespace Sentry.Android.AssemblyReader;

internal static class ArchiveUtils
{
    internal static PEReader CreatePEReader(string assemblyName, MemoryStream inputStream, DebugLogger? logger)
    {
        var decompressedStream = TryDecompressLZ4(assemblyName, inputStream, logger); // Returns null if not compressed
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
    /// The DLL may be LZ4 compressed, see https://github.com/xamarin/xamarin-android/pull/4686
    /// In particular: https://github.com/dotnet/android/blob/44c5c30d3da692c54ca27d4a41571ef20b73670f/src/Xamarin.Android.Build.Tasks/Utilities/AssemblyCompression.cs#L96-L104
    /// The format is:
    ///    [ 4 byte magic header ] (XALZ)
    ///    [ 4 byte descriptor header index ]
    ///    [ 4 byte uncompressed payload length ]
    ///    [rest: lz4 compressed payload]
    /// </summary>
    /// <seealso href="https://github.com/xamarin/xamarin-android/blob/c92702619f5fabcff0ed88e09160baf9edd70f41/tools/decompress-assemblies/main.cs#L26" />
    private static Stream? TryDecompressLZ4(string assemblyName, MemoryStream inputStream, DebugLogger? logger)
    {
        const uint compressedDataMagic = 0x5A4C4158; // 'XALZ', little-endian
        const int payloadOffset = 12;
        var reader = new BinaryReader(inputStream);
        if (reader.ReadUInt32() != compressedDataMagic)
        {
            // Restore the input stream to the beginning if we're not decompressing.
            inputStream.Position = 0;
            return null;
        }
        reader.ReadUInt32(); // ignore descriptor index, we don't need it
        var decompressedLength = reader.ReadInt32();
        Debug.Assert(inputStream.Position == payloadOffset);
        var inputLength = (int)(inputStream.Length - payloadOffset);

        logger?.Invoke("Decompressing assembly ({0} bytes uncompressed) using LZ4", decompressedLength);

        var outputStream = new MemoryStream(decompressedLength);

        // We're writing to the underlying array manually, so we need to set the length.
        outputStream.SetLength(decompressedLength);
        var outputBuffer = outputStream.GetBuffer();

        var inputBuffer = inputStream is MemorySlice slice ? slice.FullBuffer : inputStream.GetBuffer();
        var offset = inputStream is MemorySlice memorySlice ? memorySlice.Offset + payloadOffset : payloadOffset;
        var decoded = LZ4Codec.Decode(inputBuffer, offset, inputLength, outputBuffer, 0, decompressedLength);
        if (decoded != decompressedLength)
        {
            throw new Exception($"Failed to decompress LZ4 data of assembly {assemblyName} - decoded {decoded} instead of expected {decompressedLength} bytes");
        }
        return outputStream;
    }

    // Allows consumer to access the underlying buffer even if the MemoryStream is created as a slice over another.
    // Plain MemoryStream would throw "MemoryStream's internal buffer cannot be accessed."
    internal class MemorySlice(MemoryStream other, int offset, int size)
        : MemoryStream(other.GetBuffer(), offset, size, writable: false)
    {
        public readonly int Offset = offset;
        public readonly byte[] FullBuffer = other.GetBuffer();
    }
}
