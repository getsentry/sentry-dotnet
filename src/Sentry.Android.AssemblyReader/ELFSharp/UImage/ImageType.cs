namespace ELFSharp.UImage
{
    // here only supported image types are listed
    internal enum ImageType : byte
    {
        Standalone = 1,
        Kernel = 2,
        MultiFileImage = 4
    }
}
