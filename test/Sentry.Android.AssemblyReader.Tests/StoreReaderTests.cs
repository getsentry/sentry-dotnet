using Sentry.Android.AssemblyReader.V2;

namespace Sentry.Android.AssemblyReader.Tests;

public class StoreReaderTests
{
    [Fact]
    public void IsSupported_Concurrent_IsThreadSafe()
    {
        // Arrange
        var buffer = new byte[1024*1024];
        var memoryStream = new MemoryStream(buffer);
        var storeReader = new StoreReader(memoryStream, "testStore", null);

        // Act
        Parallel.For(0, 10, _ => storeReader.IsSupported());

        // No Assert - test passes if no exceptions are thrown
    }

    [Fact]
    public void ReadEntryImageData_Concurrent_IsThreadSafe()
    {
        // Arrange
        var buffer = new byte[1024*1024];
        var memoryStream = new MemoryStream(buffer);
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
        Parallel.For(0, 10, _ => storeReader.ReadEntryImageData(entry));

        // No Assert - test passes if no exceptions are thrown
    }
}
