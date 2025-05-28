using System;
using System.Collections.Generic;
using ELFSharp.ELF.Sections;
using ELFSharp.Utilities;

namespace ELFSharp.ELF.Segments
{
    internal sealed class NoteSegment<T> : Segment<T>, INoteSegment
    {
        private readonly NoteData data;

        private readonly List<NoteData> notes = new List<NoteData>();

        internal NoteSegment(long headerOffset, Class elfClass, SimpleEndianessAwareReader reader)
            : base(headerOffset, elfClass, reader)
        {
            var offset = (ulong)Offset;
            var fileSize = (ulong)FileSize;
            var remainingSize = fileSize;

            // Keep the first NoteData as a property for backwards compatibility
            data = new NoteData(offset, remainingSize, reader);
            notes.Add(data);

            offset += data.NoteFileSize;

            // Read all additional notes within the segment
            // Multiple notes are common in ELF core files
            if (data.NoteFileSize < remainingSize)
            {
                remainingSize -= data.NoteFileSize;

                while (remainingSize > NoteData.NoteDataHeaderSize)
                {
                    var note = new NoteData(offset, remainingSize, reader);
                    notes.Add(note);
                    offset += note.NoteFileSize;
                    if (note.NoteFileSize <= remainingSize)
                        remainingSize -= note.NoteFileSize;
                    else
                        // File is damaged
                        throw new IndexOutOfRangeException("NoteSegment internal note-data is out of bounds");
                }
            }
        }

        public string NoteName => data.Name;

        public ulong NoteType => data.Type;

        public byte[] NoteDescription => data.DescriptionBytes;
        public IReadOnlyList<INoteData> Notes => notes.AsReadOnly();
    }
}
