namespace ELFSharp.ELF.Segments
{
    internal interface ISegment
    {
        public SegmentType Type { get; }
        public SegmentFlags Flags { get; }
        public byte[] GetRawHeader();
        public byte[] GetFileContents();
        public byte[] GetMemoryContents();
    }
}
