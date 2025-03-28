using ELFSharp.ELF.Sections;
using ELFSharp.ELF.Segments;
using ELFSharp.Utilities;
using SectionHeader = ELFSharp.ELF.Sections.SectionHeader;

#nullable disable

namespace ELFSharp.ELF
{
    internal sealed class ELF<T> : IELF where T : struct
    {
        private const int SectionNameNotUniqueMarker = -1;
        private readonly bool ownsStream;

        private readonly SimpleEndianessAwareReader reader;
        private Stage currentStage;
        private StringTable<T> dynamicStringTable;
        private StringTable<T> objectsStringTable;
        private uint sectionHeaderEntryCount;
        private ushort sectionHeaderEntrySize;
        private long sectionHeaderOffset;
        private List<SectionHeader> sectionHeaders;
        private Dictionary<string, int> sectionIndicesByName;
        private List<Section<T>> sections;
        private ushort segmentHeaderEntryCount;
        private ushort segmentHeaderEntrySize;
        private long segmentHeaderOffset;
        private List<Segment<T>> segments;
        private uint stringTableIndex;

        internal ELF(Stream stream, bool ownsStream)
        {
            this.ownsStream = ownsStream;
            reader = ObtainEndianessAwareReader(stream);
            ReadFields();
            ReadStringTable();
            ReadSections();
            ReadSegmentHeaders();
        }

        public T EntryPoint { get; private set; }

        public T MachineFlags { get; private set; }

        public IReadOnlyList<Segment<T>> Segments => segments.AsReadOnly();

        public IReadOnlyList<Section<T>> Sections => sections.AsReadOnly();

        public Endianess Endianess { get; private set; }

        public Class Class { get; private set; }

        public FileType Type { get; private set; }

        public Machine Machine { get; private set; }

        public bool HasSegmentHeader => segmentHeaderOffset != 0;

        public bool HasSectionHeader => sectionHeaderOffset != 0;

        public bool HasSectionsStringTable => stringTableIndex != 0;

        IReadOnlyList<ISegment> IELF.Segments => Segments;

        public IStringTable SectionsStringTable { get; private set; }

        IEnumerable<TSectionType> IELF.GetSections<TSectionType>()
        {
            return Sections.Where(x => x is TSectionType).Cast<TSectionType>();
        }

        IReadOnlyList<ISection> IELF.Sections => Sections;

        bool IELF.TryGetSection(string name, out ISection section)
        {
            var result = TryGetSection(name, out var concreteSection);
            section = concreteSection;
            return result;
        }

        ISection IELF.GetSection(string name)
        {
            return GetSection(name);
        }

        bool IELF.TryGetSection(int index, out ISection section)
        {
            var result = TryGetSection(index, out var sectionConcrete);
            section = sectionConcrete;
            return result;
        }

        ISection IELF.GetSection(int index)
        {
            return GetSection(index);
        }

        public void Dispose()
        {
            if (ownsStream) reader.BaseStream.Dispose();
        }

        public IEnumerable<TSection> GetSections<TSection>() where TSection : Section<T>
        {
            return Sections.Where(x => x is TSection).Cast<TSection>();
        }

        public bool TryGetSection(string name, out Section<T> section)
        {
            return TryGetSectionInner(name, out section) == GetSectionResult.Success;
        }

        public Section<T> GetSection(string name)
        {
            var result = TryGetSectionInner(name, out var section);

            switch (result)
            {
                case GetSectionResult.Success:
                    return section;
                case GetSectionResult.SectionNameNotUnique:
                    throw new InvalidOperationException("Given section name is not unique, order is ambigous.");
                case GetSectionResult.NoSectionsStringTable:
                    throw new InvalidOperationException(
                        "Given ELF does not contain section header string table, therefore names of sections cannot be obtained.");
                case GetSectionResult.NoSuchSection:
                    throw new KeyNotFoundException(string.Format("Given section {0} could not be found in the file.",
                        name));
                default:
                    throw new InvalidOperationException("Unhandled error.");
            }
        }

        public Section<T> GetSection(int index)
        {
            var result = TryGetSectionInner(index, out var section);
            switch (result)
            {
                case GetSectionResult.Success:
                    return section;
                case GetSectionResult.NoSuchSection:
                    throw new IndexOutOfRangeException(string.Format("Given section index {0} is out of range.",
                        index));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return string.Format("[ELF: Endianess={0}, Class={1}, Type={2}, Machine={3}, EntryPoint=0x{4:X}, " +
                                 "NumberOfSections={5}, NumberOfSegments={6}]", Endianess, Class, Type, Machine,
                EntryPoint, sections.Count, segments.Count);
        }

        private bool TryGetSection(int index, out Section<T> section)
        {
            return TryGetSectionInner(index, out section) == GetSectionResult.Success;
        }

        private Section<T> GetSectionFromSectionHeader(SectionHeader header)
        {
            Section<T> returned;
            switch (header.Type)
            {
                case SectionType.Null:
                    goto default;
                case SectionType.ProgBits:
                    returned = new ProgBitsSection<T>(header, reader);
                    break;
                case SectionType.SymbolTable:
                    returned = new SymbolTable<T>(header, reader, objectsStringTable, this);
                    break;
                case SectionType.StringTable:
                    returned = new StringTable<T>(header, reader);
                    break;
                case SectionType.RelocationAddends:
                    goto default;
                case SectionType.HashTable:
                    goto default;
                case SectionType.Dynamic:
                    returned = new DynamicSection<T>(header, reader, this);
                    break;
                case SectionType.Note:
                    returned = new NoteSection<T>(header, reader);
                    break;
                case SectionType.NoBits:
                    goto default;
                case SectionType.Relocation:
                    goto default;
                case SectionType.Shlib:
                    goto default;
                case SectionType.DynamicSymbolTable:
                    returned = new SymbolTable<T>(header, reader, dynamicStringTable, this);
                    break;
                default:
                    returned = new Section<T>(header, reader);
                    break;
            }

            return returned;
        }

        private void ReadSegmentHeaders()
        {
            segments = new List<Segment<T>>(segmentHeaderEntryCount);

            for (var i = 0u; i < segmentHeaderEntryCount; i++)
            {
                var seekTo = segmentHeaderOffset + i * segmentHeaderEntrySize;
                reader.BaseStream.Seek(seekTo, SeekOrigin.Begin);
                var segmentType = Segment<T>.ProbeType(reader);

                Segment<T> segment;
                if (segmentType == SegmentType.Note)
                    segment = new NoteSegment<T>(segmentHeaderOffset + i * segmentHeaderEntrySize, Class, reader);
                else
                    segment = new Segment<T>(segmentHeaderOffset + i * segmentHeaderEntrySize, Class, reader);

                segments.Add(segment);
            }
        }

        private void ReadSections()
        {
            sectionHeaders = new List<SectionHeader>();
            if (HasSectionsStringTable) sectionIndicesByName = new Dictionary<string, int>();

            for (var i = 0; i < sectionHeaderEntryCount; i++)
            {
                var header = ReadSectionHeader(i);
                sectionHeaders.Add(header);
                if (HasSectionsStringTable)
                {
                    var name = header.Name;
                    if (!sectionIndicesByName.ContainsKey(name))
                        sectionIndicesByName.Add(name, i);
                    else
                        sectionIndicesByName[name] = SectionNameNotUniqueMarker;
                }
            }

            sections = new List<Section<T>>(Enumerable.Repeat<Section<T>>(
                null,
                sectionHeaders.Count
            ));
            FindStringTables();
            for (var i = 0; i < sectionHeaders.Count; i++) TouchSection(i);
            sectionHeaders = null;
            currentStage = Stage.AfterSectionsAreRead;
        }

        private void TouchSection(int index)
        {
            if (currentStage != Stage.Initalizing)
                throw new InvalidOperationException("TouchSection invoked in improper state.");
            if (sections[index] != null) return;
            var section = GetSectionFromSectionHeader(sectionHeaders[index]);
            sections[index] = section;
        }

        private void FindStringTables()
        {
            TryGetSection(Consts.ObjectsStringTableName, out var section);
            objectsStringTable = (StringTable<T>)section;
            TryGetSection(Consts.DynamicStringTableName, out section);

            // It might happen that the section is not really available, represented as a NoBits one.
            dynamicStringTable = section as StringTable<T>;
        }

        private void ReadStringTable()
        {
            if (!HasSectionHeader || !HasSectionsStringTable) return;

            var header = ReadSectionHeader(checked((int)stringTableIndex));
            if (header.Type != SectionType.StringTable)
                throw new InvalidOperationException(
                    "Given index of section header does not point at string table which was expected.");

            SectionsStringTable = new StringTable<T>(header, reader);
        }

        private SectionHeader ReadSectionHeader(int index, bool ignoreUpperLimit = false)
        {
            if (index < 0 || (!ignoreUpperLimit && index >= sectionHeaderEntryCount))
                throw new ArgumentOutOfRangeException(nameof(index));

            reader.BaseStream.Seek(
                sectionHeaderOffset + index * sectionHeaderEntrySize,
                SeekOrigin.Begin
            );

            return new SectionHeader(reader, Class, SectionsStringTable);
        }

        private SimpleEndianessAwareReader ObtainEndianessAwareReader(Stream stream)
        {
            var reader = new BinaryReader(stream);
            reader.ReadBytes(4); // ELF magic
            var classByte = reader.ReadByte();

            Class = classByte switch
            {
                1 => Class.Bit32,
                2 => Class.Bit64,
                _ => throw new ArgumentException($"Given ELF file is of unknown class {classByte}.")
            };

            var endianessByte = reader.ReadByte();

            Endianess = endianessByte switch
            {
                1 => Endianess.LittleEndian,
                2 => Endianess.BigEndian,
                _ => throw new ArgumentException($"Given ELF file uses unknown endianess {endianessByte}.")
            };

            reader.ReadBytes(10); // padding bytes of section e_ident
            return new SimpleEndianessAwareReader(stream, Endianess);
        }

        private void ReadFields()
        {
            Type = (FileType)reader.ReadUInt16();
            Machine = (Machine)reader.ReadUInt16();
            var version = reader.ReadUInt32();
            if (version != 1)
                throw new ArgumentException(string.Format(
                    "Given ELF file is of unknown version {0}.",
                    version
                ));
            EntryPoint = (Class == Class.Bit32 ? reader.ReadUInt32() : reader.ReadUInt64()).To<T>();
            // TODO: assertions for (u)longs
            segmentHeaderOffset = Class == Class.Bit32 ? reader.ReadUInt32() : reader.ReadInt64();
            sectionHeaderOffset = Class == Class.Bit32 ? reader.ReadUInt32() : reader.ReadInt64();
            MachineFlags = reader.ReadUInt32().To<T>(); // TODO: always 32bit?
            reader.ReadUInt16(); // elf header size
            segmentHeaderEntrySize = reader.ReadUInt16();
            segmentHeaderEntryCount = reader.ReadUInt16();
            sectionHeaderEntrySize = reader.ReadUInt16();
            sectionHeaderEntryCount = reader.ReadUInt16();
            stringTableIndex = reader.ReadUInt16();

            // If the number of sections is greater than or equal to SHN_LORESERVE (0xff00), this member has the
            // value zero and the actual number of section header table entries is contained in the sh_size field
            // of the section header at index 0. (Otherwise, the sh_size member of the initial entry contains 0.)
            if (sectionHeaderEntryCount == 0)
            {
                var firstSectionHeader = ReadSectionHeader(0, true);
                sectionHeaderEntryCount = checked((uint)firstSectionHeader.Size);

                // If the index of the string table is larger than or equal to SHN_LORESERVE (0xff00), this member holds SHN_XINDEX (0xffff)
                // and the real index of the section name string table section is held in the sh_link member of the initial entry in section
                // header table. Otherwise, the sh_link member of the initial entry in section header table contains the value zero.
                if (stringTableIndex == 0xffff) stringTableIndex = checked(firstSectionHeader.Link);
            }
        }

        private GetSectionResult TryGetSectionInner(string name, out Section<T> section)
        {
            section = default;
            if (!HasSectionsStringTable) return GetSectionResult.NoSectionsStringTable;
            if (!sectionIndicesByName.TryGetValue(name, out var index)) return GetSectionResult.NoSuchSection;
            if (index == SectionNameNotUniqueMarker) return GetSectionResult.SectionNameNotUnique;
            return TryGetSectionInner(index, out section);
        }

        private GetSectionResult TryGetSectionInner(int index, out Section<T> section)
        {
            section = default;
            if (index >= sections.Count) return GetSectionResult.NoSuchSection;
            if (sections[index] != null)
            {
                section = sections[index];
                return GetSectionResult.Success;
            }

            if (currentStage != Stage.Initalizing)
                throw new InvalidOperationException(
                    "Assert not met: null section by proper index in not initializing stage.");
            TouchSection(index);
            section = sections[index];
            return GetSectionResult.Success;
        }

        private enum Stage
        {
            Initalizing,
            AfterSectionsAreRead
        }

        private enum GetSectionResult
        {
            Success,
            SectionNameNotUnique,
            NoSectionsStringTable,
            NoSuchSection
        }
    }
}
