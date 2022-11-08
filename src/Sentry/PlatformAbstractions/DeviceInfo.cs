namespace Sentry.PlatformAbstractions;

internal static class DeviceInfo
{
#if ANDROID
        public const string PlatformName = "Android";
#elif IOS
        public const string PlatformName = "iOS";
#elif MACCATALYST
        public const string PlatformName = "Mac Catalyst";
#endif
}
