using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

#nullable disable

namespace ELFSharp.MachO
{
    internal static class MachOReader
    {
        private const uint FatMagic = 0xBEBAFECA;

        private const string FatArchiveErrorMessage =
            "Given file is a fat archive, contains more than one MachO binary. Use (Try)LoadFat to handle it.";

        private const string NotMachOErrorMessage = "Given file is not a Mach-O file.";

        private static readonly IReadOnlyDictionary<uint, (bool Is64Bit, Endianess Endianess)> MagicToMachOType =
            new Dictionary<uint, (bool, Endianess)>
            {
                { 0xFEEDFACE, (false, Endianess.LittleEndian) },
                { 0xFEEDFACF, (true, Endianess.LittleEndian) },
                { 0xCEFAEDFE, (false, Endianess.BigEndian) },
                { 0xCFFAEDFE, (true, Endianess.BigEndian) }
            };

        public static MachO Load(string fileName)
        {
            return Load(File.OpenRead(fileName), true);
        }

        public static MachO Load(Stream stream, bool shouldOwnStream)
        {
            return TryLoad(stream, shouldOwnStream, out var result) switch
            {
                MachOResult.OK => result,
                MachOResult.NotMachO => throw new InvalidOperationException(NotMachOErrorMessage),
                MachOResult.FatMachO => throw new InvalidOperationException(FatArchiveErrorMessage),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static IReadOnlyList<MachO> LoadFat(Stream stream, bool shouldOwnStream)
        {
            var result = TryLoadFat(stream, shouldOwnStream, out var machOs);
            if (result == MachOResult.OK || result == MachOResult.FatMachO) return machOs;

            throw new InvalidOperationException(NotMachOErrorMessage);
        }

        public static MachOResult TryLoad(string fileName, out MachO machO)
        {
            return TryLoad(File.OpenRead(fileName), true, out machO);
        }

        public static MachOResult TryLoad(Stream stream, bool shouldOwnStream, out MachO machO)
        {
            var result = TryLoadFat(stream, shouldOwnStream, out var machOs);

            if (result == MachOResult.OK)
                machO = machOs.SingleOrDefault();
            else
                machO = null;

            return result;
        }

        public static MachOResult TryLoadFat(Stream stream, bool shouldOwnStream, out IReadOnlyList<MachO> machOs)
        {
            machOs = null;

            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            var magic = reader.ReadUInt32();

            if (magic == FatMagic)
            {
                machOs = FatArchiveReader.Enumerate(stream, shouldOwnStream).ToArray();
                return MachOResult.FatMachO;
            }

            if (!MagicToMachOType.TryGetValue(magic, out var machOType)) return MachOResult.NotMachO;

            var machO = new MachO(stream, machOType.Is64Bit, machOType.Endianess, shouldOwnStream);
            machOs = new[] { machO };
            return MachOResult.OK;
        }
    }
}
