namespace Sentry.Quartz;

internal static class RegionInfoExtensions
{
    extension(RegionInfo)
    {
        public static RegionInfo? GetCurrentRegionOrNull()
        {
            try
            {
                return RegionInfo.CurrentRegion;
            }
            catch (CultureNotFoundException)
            {
                return null;
            }
        }
    }
}