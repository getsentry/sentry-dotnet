namespace Sentry.Quartz.Tests;

public class RegionInfoExtensionsTests
{
    [Fact]
    public void GetCurrentRegionOrNull_DoesNotThrow_AndMatchesCurrentRegion()
    {
        var result = RegionInfo.GetCurrentRegionOrNull();

        result.Should().NotBeNull();
        result!.TwoLetterISORegionName.Should().Be(RegionInfo.CurrentRegion.TwoLetterISORegionName);
    }
}
