using Sentry.Android.AssemblyReader.V2;

namespace Sentry.Android.AssemblyReader.Tests;

public class StoreReaderTests
{
    [Theory]
    [InlineData(0x80000003u | 0x00010000u, true, sizeof(ulong))] // v3, 64-bit, arm64
    [InlineData(0x00000003u | 0x00020000u, false, sizeof(uint))] // v3, 32-bit, arm
    [InlineData(0x80000004u | 0x00030000u, true, sizeof(ulong))] // v4, 64-bit, x86_64 (MonoVM)
    [InlineData(0x80000004u | 0x00030000u, true, sizeof(uint))] // v4, 64-bit, x86_64 (CoreCLR)
    [InlineData(0x00000004u | 0x00040000u, false, sizeof(uint))] // v4, 32-bit, x86
    public void Create_SupportedVersion_ReadsStore(uint version, bool is64Bit, int nameHashSize)
    {
        // Arrange
        using var stream = CreateStore(version, nameHashSize, "testAssembly.dll");

        // Act
        var reader = AssemblyStoreReader.Create(stream, "testStore", null);

        // Assert
        reader.Should().NotBeNull();
        reader!.Is64Bit.Should().Be(is64Bit);
        reader.Assemblies.Should().ContainSingle().Which.Name.Should().Be("testAssembly.dll");
    }

    [Theory]
    [InlineData(0x80000005u | 0x00010000u)] // v5
    [InlineData(0x80000002u | 0x00010000u)] // v2
    public void Create_UnsupportedVersion_ReturnsNull(uint version)
    {
        // Arrange
        using var stream = CreateStore(version, sizeof(ulong), "testAssembly.dll");

        // Act
        var reader = AssemblyStoreReader.Create(stream, "testStore", null);

        // Assert
        reader.Should().BeNull();
    }

    [Theory]
    [InlineData(12u, 1u)] // v2 layout: 64-bit hash, no ignore flag
    [InlineData(17u, 2u)] // not evenly divisible
    public void Create_InvalidIndexSize_Throws(uint indexSize, uint indexEntryCount)
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            WriteHeader(writer, 0x80000004u | 0x00010000u, indexEntryCount, indexSize);
            writer.Write(new byte[indexSize]);
        }
        stream.Position = 0;

        // Act
        var act = () => AssemblyStoreReader.Create(stream, "testStore", null);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*testStore*index*");
    }

    /// <summary>
    /// Builds a minimal assembly store with a single assembly, using the v3/v4 layout.
    /// </summary>
    private static MemoryStream CreateStore(uint version, int nameHashSize, string assemblyName)
    {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            var indexEntrySize = nameHashSize + sizeof(uint) + sizeof(byte);
            WriteHeader(writer, version, indexEntryCount: 1, (uint)indexEntrySize);

            // Index
            if (nameHashSize == sizeof(ulong))
            {
                writer.Write(0xDEADBEEFDEADBEEFul); // name_hash
            }
            else
            {
                writer.Write(0xDEADBEEFu); // name_hash
            }
            writer.Write(0u); // descriptor_index
            writer.Write((byte)0); // ignore

            // Descriptor: mapping_index, data offset/size, debug offset/size, config offset/size
            for (var i = 0; i < 7; i++)
            {
                writer.Write(0u);
            }

            // Names
            var nameBytes = Encoding.UTF8.GetBytes(assemblyName);
            writer.Write((uint)nameBytes.Length);
            writer.Write(nameBytes);
        }

        stream.Position = 0;
        return stream;
    }

    private static void WriteHeader(BinaryWriter writer, uint version, uint indexEntryCount, uint indexSize)
    {
        writer.Write(Utils.AssemblyStoreMagic);
        writer.Write(version);
        writer.Write(1u); // entry_count
        writer.Write(indexEntryCount);
        writer.Write(indexSize);
        if ((version & 0xFFFF) >= 4)
        {
            writer.Write(0x0123456789ABCDEFul); // content_id
        }
    }

    [Fact]
    public void IsSupported_Concurrent_IsThreadSafe()
    {
        // Arrange
        var buffer = new byte[1024 * 1024];
        using var memoryStream = new MemoryStream(buffer);
        var storeReader = new StoreReader(memoryStream, "testStore", null);

        // Act
        var result = Parallel.For(0, 10, _ => storeReader.IsSupported());

        // Test passes if no exceptions are thrown, but we can also assert completion
        result.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void ReadEntryImageData_Concurrent_IsThreadSafe()
    {
        // Arrange
        var buffer = new byte[1024 * 1024];
        using var memoryStream = new MemoryStream(buffer);
        var storeReader = new StoreReader(memoryStream, "testStore", null);
        var entry = new StoreReader.StoreItemV2(
            AndroidTargetArch.Arm64,
            "testAssembly.dll",
            is64Bit: true,
            new List<StoreReader.IndexEntry>(),
            new StoreReader.EntryDescriptor
            {
                data_offset = 0,
                data_size = 0,
                debug_data_offset = 0,
                debug_data_size = 0,
                config_data_offset = 0,
                config_data_size = 0,
                mapping_index = 0
            },
            ignore: false);

        // Act
        var result = Parallel.For(0, 10, _ => storeReader.ReadEntryImageData(entry));

        // Test passes if no exceptions are thrown, but we can also assert completion
        result.IsCompleted.Should().BeTrue();
    }
}
