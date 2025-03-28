using System;
using System.IO;
using System.Net;
using System.Text;

#nullable disable

namespace ELFSharp.UImage
{
    internal static class UImageReader
    {
        private const uint Magic = 0x27051956;
        private const uint Polynomial = 0xEDB88320;
        private const uint Seed = 0xFFFFFFFF;

        public static UImage Load(string fileName)
        {
            return Load(File.OpenRead(fileName), true);
        }

        public static UImage Load(Stream stream, bool shouldOwnStream)
        {
            return TryLoad(stream, shouldOwnStream, out var result) switch
            {
                UImageResult.OK => result,
                UImageResult.NotUImage => throw new InvalidOperationException("Given file is not an UBoot image."),
                UImageResult.BadChecksum => throw new InvalidOperationException(
                    "Wrong header checksum of the given UImage file."),
                UImageResult.NotSupportedImageType => throw new InvalidOperationException(
                    "Given image type is not supported."),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static UImageResult TryLoad(string fileName, out UImage uImage)
        {
            return TryLoad(File.OpenRead(fileName), true, out uImage);
        }

        public static UImageResult TryLoad(Stream stream, bool shouldOwnStream, out UImage uImage)
        {
            var startingStreamPosition = stream.Position;

            uImage = null;
            if (stream.Length < 64)
                return UImageResult.NotUImage;

            using var reader = new BinaryReader(stream, Encoding.UTF8, true);

            var headerForCrc = reader.ReadBytes(64);
            // we need to zero crc part
            for (var i = 4; i < 8; i++)
                headerForCrc[i] = 0;

            stream.Position = startingStreamPosition;

            var magic = reader.ReadUInt32BigEndian();
            if (magic != Magic)
                return UImageResult.NotUImage;

            var crc = reader.ReadUInt32BigEndian();
            if (crc != GzipCrc32(headerForCrc))
                return UImageResult.BadChecksum;

            reader.ReadBytes(22);
            var imageType = (ImageType)reader.ReadByte();
            if (!Enum.IsDefined(typeof(ImageType), imageType))
                return UImageResult.NotSupportedImageType;

            var multiFileImage = imageType == ImageType.MultiFileImage;
            stream.Position = startingStreamPosition;
            uImage = new UImage(stream, multiFileImage, shouldOwnStream);
            return UImageResult.OK;
        }

        internal static uint GzipCrc32(byte[] data)
        {
            var remainder = Seed;
            for (var i = 0; i < data.Length; i++)
            {
                remainder ^= data[i];
                for (var j = 0; j < 8; j++)
                    if ((remainder & 1) != 0)
                        remainder = (remainder >> 1) ^ Polynomial;
                    else
                        remainder >>= 1;
            }

            return remainder ^ Seed;
        }

        internal static uint ReadUInt32BigEndian(this BinaryReader reader)
        {
            return (uint)IPAddress.HostToNetworkOrder(reader.ReadInt32());
        }

        internal static int ReadInt32BigEndian(this BinaryReader reader)
        {
            return IPAddress.HostToNetworkOrder(reader.ReadInt32());
        }
    }
}
