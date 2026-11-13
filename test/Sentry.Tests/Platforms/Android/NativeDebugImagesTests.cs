#if ANDROID
using Sentry.Android;

namespace Sentry.Tests.Platforms.Android;

public class NativeDebugImagesTests
{
    [Fact]
    public void Convert_ElfModule_KeyedByImageAddress()
    {
        var module = new IO.Sentry.Ndk.DebugImage
        {
            Type = "elf",
            ImageAddr = "0x7b2f400000",
            CodeId = "a1b2c3d4e5f60718293a4b5c6d7e8f9011223344",
            DebugId = "d4c3b2a1-f6e5-1807-293a-4b5c6d7e8f90",
            CodeFile = "/data/app/com.example/split_config.arm64_v8a.apk!/lib/arm64-v8a/libExample.so",
        };
        module.SetImageSize(4096L);

        var images = NativeDebugImages.Convert([module]);

        var image = Assert.Single(images);
        Assert.Equal(0x7b2f400000, image.Key);
        Assert.Equal("elf", image.Value.Type);
        Assert.Equal(0x7b2f400000, image.Value.ImageAddress);
        Assert.Equal(4096, image.Value.ImageSize);
        Assert.Equal(module.CodeId, image.Value.CodeId);
        Assert.Equal(module.DebugId, image.Value.DebugId);
        Assert.Equal(module.CodeFile, image.Value.CodeFile);
    }

    [Fact]
    public void Convert_ModuleWithoutImageAddress_Skipped()
    {
        var images = NativeDebugImages.Convert([new IO.Sentry.Ndk.DebugImage { Type = "elf" }]);

        Assert.Empty(images);
    }
}
#endif
