namespace ELFSharp.UImage
{
    internal enum CompressionType : byte
    {
        None = 0,
        Gzip = 1,
        Bzip2 = 2,
        Lzma = 3,
        Lzo = 4
    }
}
