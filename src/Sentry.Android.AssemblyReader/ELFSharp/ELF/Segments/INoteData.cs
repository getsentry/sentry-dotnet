using System.Collections.ObjectModel;
using System.IO;

namespace ELFSharp.ELF.Segments
{
    internal interface INoteData
    {
        /// <summary>
        ///     Owner of the note.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Data contents of the note. The format of this depends on the combination of the Name and Type properties and often
        ///     corresponds to a struct.
        ///     For example, see elf.h in the Linux kernel source tree.
        /// </summary>
        public ReadOnlyCollection<byte> Description { get; }

        /// <summary>
        ///     Data type
        /// </summary>
        public ulong Type { get; }

        /// <summary>
        ///     Returns the Description byte[] as a Stream
        /// </summary>
        /// <returns></returns>
        public Stream ToStream();
    }
}
