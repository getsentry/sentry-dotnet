using Sentry.Android.AssemblyReader.V2;

namespace Sentry.Android.AssemblyReader.Tests;

public class StoreReaderTests
{
    [Theory]
    [InlineData(0x80000003u | 0x00010000u, true)] // v3, 64-bit, arm64
    [InlineData(0x00000003u | 0x00020000u, false)] // v3, 32-bit, arm
    [InlineData(0x80000004u | 0x00030000u, true)] // v4, 64-bit, x86_64
    [InlineData(0x00000004u | 0x00040000u, false)] // v4, 32-bit, x86
    public void Create_SupportedVersion_ReadsStore(uint version, bool is64Bit)
    {
        // Arrange
        using var stream = CreateStore(version, is64Bit, "testAssembly.dll");

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
        using var stream = CreateStore(version, is64Bit: true, "testAssembly.dll");

        // Act
        var reader = AssemblyStoreReader.Create(stream, "testStore", null);

        // Assert
        reader.Should().BeNull();
    }

    private static MemoryStream CreateStore(uint version, bool is64Bit, string assemblyName)
    {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            var indexEntrySize = (is64Bit ? sizeof(ulong) : sizeof(uint)) + sizeof(uint) + sizeof(byte);

            // Header
            writer.Write(Utils.AssemblyStoreMagic);
            writer.Write(version);
            writer.Write(1u); // entry_count
            writer.Write(1u); // index_entry_count
            writer.Write((uint)indexEntrySize); // index_size
            if ((version & 0xFFFF) >= 4)
            {
                writer.Write(0x0123456789ABCDEFul); // content_id
            }

            // Index
            if (is64Bit)
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
