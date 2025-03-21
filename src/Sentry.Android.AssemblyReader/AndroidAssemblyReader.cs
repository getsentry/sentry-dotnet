namespace Sentry.Android.AssemblyReader;

internal abstract class AndroidAssemblyReader : IDisposable
{
    protected DebugLogger? Logger { get; }
    protected ZipArchive ZipArchive { get; }
    protected IList<string> SupportedAbis { get; }

    protected AndroidAssemblyReader(ZipArchive zip, IList<string> supportedAbis, DebugLogger? logger)
    {
        ZipArchive = zip;
        Logger = logger;
        SupportedAbis = supportedAbis;
    }

    public void Dispose()
    {
        ZipArchive.Dispose();
    }

    internal static PEReader CreatePEReader(string assemblyName, MemoryStream inputStream, DebugLogger? logger)
    {
        var decompressedStream = TryDecompressLZ4(assemblyName, inputStream, logger);

        // Use the decompressed stream, or if null, i.e. it wasn't compressed, use the original.
        return new PEReader(decompressedStream ?? inputStream);
    }

    /// <summary>
    /// The DLL may be LZ4 compressed, see https://github.com/xamarin/xamarin-android/pull/4686
    /// The format is:
    ///    [ 4 byte magic header ] (XALZ)
    ///    [ 4 byte header index ]
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
    protected class MemorySlice : MemoryStream
    {
        public readonly int Offset;
        public readonly byte[] FullBuffer;

        public MemorySlice(MemoryStream other, int offset, int size) : base(other.GetBuffer(), offset, size, writable: false)
        {
            Offset = offset;
            FullBuffer = other.GetBuffer();
        }
    }
}
