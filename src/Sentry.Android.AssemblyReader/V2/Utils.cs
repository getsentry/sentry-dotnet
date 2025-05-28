/*
 * Adapted from https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/tools/assembly-store-reader-mk2/AssemblyStore/Utils.cs
 * Original code licensed under the MIT License (https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/LICENSE.TXT)
 */
using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using Machine = ELFSharp.ELF.Machine;

namespace Sentry.Android.AssemblyReader.V2;

internal static class Utils
{
    private static readonly string[] AabZipEntries = {
        "base/manifest/AndroidManifest.xml",
        "BundleConfig.pb",
    };
    private static readonly string[] AabBaseZipEntries = {
        "manifest/AndroidManifest.xml",
    };
    private static readonly string[] ApkZipEntries = {
        "AndroidManifest.xml",
    };

    public const uint ZipMagic = 0x4034b50;
    public const uint AssemblyStoreMagic = 0x41424158;
    public const uint ELFMagic = 0x464c457f;

    public static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;

    public static (ulong offset, ulong size, ELFPayloadError error) FindELFPayloadSectionOffsetAndSize(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        var elfClass = ELFReader.CheckELFType(stream);
        if (elfClass == Class.NotELF)
        {
            return ReturnError(null, ELFPayloadError.NotELF);
        }

        if (!ELFReader.TryLoad(stream, shouldOwnStream: false, out IELF? elf))
        {
            return ReturnError(elf, ELFPayloadError.LoadFailed);
        }

        if (elf.Type != FileType.SharedObject)
        {
            return ReturnError(elf, ELFPayloadError.NotSharedLibrary);
        }

        if (elf.Endianess != ELFSharp.Endianess.LittleEndian)
        {
            return ReturnError(elf, ELFPayloadError.NotLittleEndian);
        }

        if (!elf.TryGetSection("payload", out ISection? payloadSection))
        {
            return ReturnError(elf, ELFPayloadError.NoPayloadSection);
        }

        var is64 = elf.Machine switch
        {
            Machine.ARM => false,
            Machine.Intel386 => false,

            Machine.AArch64 => true,
            Machine.AMD64 => true,

            _ => throw new NotSupportedException($"Unsupported ELF architecture '{elf.Machine}'")
        };

        ulong offset;
        ulong size;

        if (is64)
        {
            (offset, size) = GetOffsetAndSize64((Section<ulong>)payloadSection);
        }
        else
        {
            (offset, size) = GetOffsetAndSize32((Section<uint>)payloadSection);
        }

        elf.Dispose();
        return (offset, size, ELFPayloadError.None);

        (ulong offset, ulong size) GetOffsetAndSize64(Section<ulong> payload)
        {
            return (payload.Offset, payload.Size);
        }

        (ulong offset, ulong size) GetOffsetAndSize32(Section<uint> payload)
        {
            return ((ulong)payload.Offset, (ulong)payload.Size);
        }

        (ulong offset, ulong size, ELFPayloadError error) ReturnError(IELF? elf, ELFPayloadError error)
        {
            elf?.Dispose();

            return (0, 0, error);
        }
    }

    public static (FileFormat format, FileInfo? info) DetectFileFormat(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return (FileFormat.Unknown, null);
        }

        var info = new FileInfo(path);
        if (!info.Exists)
        {
            return (FileFormat.Unknown, null);
        }

        using var reader = new BinaryReader(info.OpenRead());

        // ATM, all formats we recognize have 4-byte magic at the start
        var format = reader.ReadUInt32() switch
        {
            ZipMagic => FileFormat.Zip,
            ELFMagic => FileFormat.ELF,
            AssemblyStoreMagic => FileFormat.AssemblyStore,
            _ => FileFormat.Unknown
        };

        if (format == FileFormat.Unknown || format != FileFormat.Zip)
        {
            return (format, info);
        }

        return (DetectAndroidArchive(info, format), info);
    }

    private static FileFormat DetectAndroidArchive(FileInfo info, FileFormat defaultFormat)
    {
        using var zip = ZipFile.OpenRead(info.FullName);

        if (HasAllEntries(zip, AabZipEntries))
        {
            return FileFormat.Aab;
        }

        if (HasAllEntries(zip, ApkZipEntries))
        {
            return FileFormat.Apk;
        }

        if (HasAllEntries(zip, AabBaseZipEntries))
        {
            return FileFormat.AabBase;
        }

        return defaultFormat;

        static bool HasAllEntries(ZipArchive zip, string[] entries)
        {
            foreach (var entry in entries)
            {
                if (zip.GetEntry(entry) is null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
