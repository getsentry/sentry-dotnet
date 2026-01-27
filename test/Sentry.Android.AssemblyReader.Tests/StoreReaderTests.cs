using Sentry.Android.AssemblyReader.V2;

namespace Sentry.Android.AssemblyReader.Tests;

public class StoreReaderTests
{
    [Fact]
    public void IsSupported_Concurrent_IsThreadSafe()
    {
        // Arrange
        var buffer = new byte[1024 * 1024];
        using var memoryStream = new MemoryStream(buffer);
        var storeReader = new TestStoreReader(memoryStream, "testStore", null);

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

file sealed class TestStoreReader : StoreReader
{
    public TestStoreReader(Stream store, string path, DebugLogger? logger)
        : base(store, path, logger)
    {
    }

    public new bool IsSupported()
    {
        return base.IsSupported();
    }
}
