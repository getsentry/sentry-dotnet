using System.Collections.Generic;

namespace ELFSharp.ELF.Segments
{
    internal interface INoteSegment : ISegment
    {
        string NoteName { get; }
        ulong NoteType { get; }
        byte[] NoteDescription { get; }

        /// <summary>
        ///     Returns all notes within the segment
        /// </summary>
        IReadOnlyList<INoteData> Notes { get; }
    }
}
