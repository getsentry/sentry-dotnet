using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

#nullable disable

namespace ELFSharp.UImage
{
    internal sealed class UImage
    {
        private const int MaximumNameLength = 32;
        private readonly List<int> imageSizes;
        private readonly byte[] rawImage;
        private readonly bool shouldOwnStream;

        internal UImage(Stream stream, bool multiFileImage, bool ownsStream)
        {
            shouldOwnStream = ownsStream;
            imageSizes = new List<int>();

            using var reader = new BinaryReader(stream, Encoding.UTF8, !ownsStream);

            reader.ReadBytes(8); // magic and CRC, already checked
            Timestamp = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) +
                         TimeSpan.FromSeconds(reader.ReadInt32BigEndian())).ToLocalTime();
            Size = reader.ReadUInt32BigEndian();
            LoadAddress = reader.ReadUInt32BigEndian();
            EntryPoint = reader.ReadUInt32BigEndian();
            CRC = reader.ReadUInt32BigEndian();
            OperatingSystem = (OS)reader.ReadByte();
            Architecture = (Architecture)reader.ReadByte();
            Type = (ImageType)reader.ReadByte();
            Compression = (CompressionType)reader.ReadByte();
            var nameAsBytes = reader.ReadBytes(32);
            Name = Encoding.UTF8.GetString(nameAsBytes.Reverse().SkipWhile(x => x == 0).Reverse().ToArray());

            if (multiFileImage)
            {
                var startingPosition = stream.Position;

                int nextImageSize;
                do
                {
                    nextImageSize = reader.ReadInt32BigEndian();
                    imageSizes.Add(nextImageSize);
                } while (nextImageSize != 0);

                // Last image size is actually a terminator.
                imageSizes.RemoveAt(imageSizes.Count - 1);
                ImageCount = imageSizes.Count;
                stream.Position = startingPosition;
            }

            rawImage = reader.ReadBytes((int)Size);
        }

        public uint CRC { get; }
        public bool IsChecksumOK { get; private set; }
        public uint Size { get; }
        public uint LoadAddress { get; private set; }
        public uint EntryPoint { get; private set; }
        public string Name { get; private set; }
        public DateTime Timestamp { get; private set; }
        public CompressionType Compression { get; }
        public ImageType Type { get; private set; }
        public OS OperatingSystem { get; private set; }
        public Architecture Architecture { get; private set; }
        public int ImageCount { get; }

        public ImageDataResult TryGetImageData(int imageIndex, out byte[] result)
        {
            result = null;

            if (imageIndex > ImageCount - 1 || imageIndex < 0)
                return ImageDataResult.InvalidIndex;

            if (ImageCount == 1)
                return TryGetImageData(out result);

            if (Compression != CompressionType.None)
                // We only support multi file images without compression
                return ImageDataResult.UnsupportedCompressionFormat;

            if (CRC != UImageReader.GzipCrc32(rawImage))
                return ImageDataResult.BadChecksum;

            // Images sizes * 4 + terminator (which also takes 4 bytes).
            var startingOffset = 4 * (ImageCount + 1) + imageSizes.Take(imageIndex).Sum();
            result = new byte[imageSizes[imageIndex]];
            Array.Copy(rawImage, startingOffset, result, 0, result.Length);

            return ImageDataResult.OK;
        }

        public ImageDataResult TryGetImageData(out byte[] result)
        {
            result = null;

            if (ImageCount > 1)
                return TryGetImageData(0, out result);

            if (Compression != CompressionType.None && Compression != CompressionType.Gzip)
                return ImageDataResult.UnsupportedCompressionFormat;

            if (CRC != UImageReader.GzipCrc32(rawImage))
                return ImageDataResult.BadChecksum;

            result = new byte[rawImage.Length];
            Array.Copy(rawImage, result, result.Length);
            if (Compression == CompressionType.Gzip)
                using (var stream = new GZipStream(new MemoryStream(result), CompressionMode.Decompress))
                {
                    using (var decompressed = new MemoryStream())
                    {
                        stream.CopyTo(decompressed);
                        result = decompressed.ToArray();
                    }
                }

            return ImageDataResult.OK;
        }

        public byte[] GetImageData(int imageIndex)
        {
            byte[] result;
            var imageDataResult = TryGetImageData(imageIndex, out result);
            return InterpretImageResult(result, imageDataResult);
        }

        public byte[] GetImageData()
        {
            byte[] result;
            var imageDataResult = TryGetImageData(out result);
            return InterpretImageResult(result, imageDataResult);
        }

        private byte[] InterpretImageResult(byte[] result, ImageDataResult imageDataResult)
        {
            return imageDataResult switch
            {
                ImageDataResult.OK => result,
                ImageDataResult.BadChecksum => throw new InvalidOperationException(
                    "Bad checksum of the image, probably corrupted image."),
                ImageDataResult.UnsupportedCompressionFormat => throw new InvalidOperationException(
                    string.Format("Unsupported compression format '{0}'.", Compression)),
                ImageDataResult.InvalidIndex => throw new ArgumentException("Invalid image index."),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public byte[] GetRawImageData()
        {
            var result = new byte[rawImage.Length];
            Array.Copy(rawImage, result, result.Length);
            return result;
        }
    }
}
