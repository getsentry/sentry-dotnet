/*
 * Adapted from https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/tools/assembly-store-reader-mk2/AssemblyStore/AssemblyStoreReader.cs
 * Original code licensed under the MIT License (https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/LICENSE.TXT)
 */

namespace Sentry.Android.AssemblyReader.V2;

internal abstract class AssemblyStoreReader
{
    protected DebugLogger? Logger { get; }

    private static readonly UTF8Encoding ReaderEncoding = new UTF8Encoding(false);

    internal Lock StreamLock { get; } = new();
    protected Stream StoreStream { get; }

    public abstract string Description { get; }
    public abstract bool NeedsExtensionInName { get; }
    public string StorePath { get; }

    public AndroidTargetArch TargetArch { get; protected set; } = AndroidTargetArch.Arm;
    public uint AssemblyCount { get; protected set; }
    public uint IndexEntryCount { get; protected set; }
    public IList<AssemblyStoreItem>? Assemblies { get; protected set; }
    public bool Is64Bit { get; protected set; }

    protected AssemblyStoreReader(Stream store, string path, DebugLogger? logger)
    {
        StoreStream = store;
        StorePath = path;
        Logger = logger;
    }

    public static AssemblyStoreReader? Create(Stream store, string path, DebugLogger? logger)
    {
        var reader = new StoreReader(store, path, logger);
        if (!reader.IsSupported())
        {
            return null;
        }

        reader.Prepare();
        return reader;
    }

    protected BinaryReader CreateReader() => new BinaryReader(StoreStream, ReaderEncoding, leaveOpen: true);

    protected abstract bool IsSupported();
    protected abstract void Prepare();
    protected abstract ulong GetStoreStartDataOffset();

    public MemoryStream ReadEntryImageData(AssemblyStoreItem entry, bool uncompressIfNeeded = false)
    {
        lock (StreamLock)
        {
            var startOffset = GetStoreStartDataOffset();
            StoreStream.Seek((uint)startOffset + entry.DataOffset, SeekOrigin.Begin);
            var stream = new MemoryStream();

            if (uncompressIfNeeded)
            {
                throw new NotImplementedException();
            }

            const long bufferSize = 65535;
            var buffer = Utils.BytePool.Rent((int)bufferSize);
            try
            {
                long remainingToRead = entry.DataSize;

                while (remainingToRead > 0)
                {
                    var nread = StoreStream.Read(buffer, 0, (int)Math.Min(bufferSize, remainingToRead));
                    stream.Write(buffer, 0, nread);
                    remainingToRead -= (long)nread;
                }
                stream.Flush();
                stream.Seek(0, SeekOrigin.Begin);
            }
            finally
            {
                Utils.BytePool.Return(buffer);
            }

            return stream;
        }
    }
}
