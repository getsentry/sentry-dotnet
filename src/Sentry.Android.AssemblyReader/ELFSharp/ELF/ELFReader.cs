using System;
using System.IO;
using System.Text;

#nullable disable

namespace ELFSharp.ELF
{
    internal static class ELFReader
    {
        private const string NotELFMessage = "Given stream is not a proper ELF file.";

        private static readonly byte[] Magic =
        {
            0x7F,
            0x45,
            0x4C,
            0x46
        }; // 0x7F 'E' 'L' 'F'

        public static IELF Load(Stream stream, bool shouldOwnStream)
        {
            if (!TryLoad(stream, shouldOwnStream, out var elf)) throw new ArgumentException(NotELFMessage);

            return elf;
        }

        public static IELF Load(string fileName)
        {
            return Load(File.OpenRead(fileName), true);
        }

        public static bool TryLoad(Stream stream, bool shouldOwnStream, out IELF elf)
        {
            switch (CheckELFType(stream))
            {
                case Class.Bit32:
                    elf = new ELF<uint>(stream, shouldOwnStream);
                    return true;
                case Class.Bit64:
                    elf = new ELF<ulong>(stream, shouldOwnStream);
                    return true;
                default:
                    elf = null;
                    stream.Close();
                    return false;
            }
        }

        public static bool TryLoad(string fileName, out IELF elf)
        {
            return TryLoad(File.OpenRead(fileName), true, out elf);
        }

        public static Class CheckELFType(Stream stream)
        {
            var currentStreamPosition = stream.Position;

            if (stream.Length < Consts.MinimalELFSize) return Class.NotELF;

            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                var magic = reader.ReadBytes(4);
                for (var i = 0; i < 4; i++)
                    if (magic[i] != Magic[i])
                        return Class.NotELF;

                var value = reader.ReadByte();
                stream.Position = currentStreamPosition;
                return value == 1 ? Class.Bit32 : Class.Bit64;
            }
        }

        public static Class CheckELFType(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                return CheckELFType(stream);
            }
        }

        public static ELF<T> Load<T>(Stream stream, bool shouldOwnStream) where T : struct
        {
            if (CheckELFType(stream) == Class.NotELF) throw new ArgumentException(NotELFMessage);

            return new ELF<T>(stream, shouldOwnStream);
        }

        public static ELF<T> Load<T>(string fileName) where T : struct
        {
            return Load<T>(File.OpenRead(fileName), true);
        }

        public static bool TryLoad<T>(Stream stream, bool shouldOwnStream, out ELF<T> elf) where T : struct
        {
            switch (CheckELFType(stream))
            {
                case Class.Bit32:
                case Class.Bit64:
                    elf = new ELF<T>(stream, shouldOwnStream);
                    return true;
                default:
                    elf = null;
                    return false;
            }
        }

        public static bool TryLoad<T>(string fileName, out ELF<T> elf) where T : struct
        {
            return TryLoad(File.OpenRead(fileName), true, out elf);
        }
    }
}
