#if ANDROID
using Sentry.Android;
using AndroidBuild = Android.OS.Build;

namespace Sentry.Tests.Platforms.Android;

public class AndroidDeviceLogAttributesTests
{
    [Fact]
    public void Create_ReadsBuildValues()
    {
        var attributes = AndroidDeviceLogAttributes.Create().ToDictionary(a => a.Key, a => a.Value);

        attributes.Should().Contain("os.name", "Android");
        attributes.Should().Contain("os.version", AndroidBuild.VERSION.Release);
        attributes.Should().Contain("device.brand", AndroidBuild.Brand);
        attributes.Should().Contain("device.model", AndroidBuild.Model);
        attributes.Should().Contain("device.family", AndroidBuild.Model!.Split(' ')[0]);
    }
}
#endif
