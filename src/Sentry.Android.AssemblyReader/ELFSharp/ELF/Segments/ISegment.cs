namespace ELFSharp.ELF.Segments
{
    internal interface ISegment
    {
        SegmentType Type { get; }
        SegmentFlags Flags { get; }
        byte[] GetRawHeader();
        byte[] GetFileContents();
        byte[] GetMemoryContents();
    }
}
