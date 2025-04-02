using System.Collections.Generic;

namespace ELFSharp.ELF.Segments
{
    internal interface INoteSegment : ISegment
    {
        public string NoteName { get; }
        public ulong NoteType { get; }
        public byte[] NoteDescription { get; }

        /// <summary>
        ///     Returns all notes within the segment
        /// </summary>
        public IReadOnlyList<INoteData> Notes { get; }
    }
}
