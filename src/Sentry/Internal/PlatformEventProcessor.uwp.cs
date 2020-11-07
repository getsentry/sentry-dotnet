#if WINDOWS_UWP
using System;
using Sentry.Extensibility;
using Windows.ApplicationModel;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.System.Profile;

namespace Sentry.Internal
{

    internal class PlatformEventProcessor : ISentryEventProcessor
    {
        private Lazy<UwpContext> _uwpContext = new Lazy<UwpContext>(() => new UwpContext());
        private volatile bool _uwpContextLoaded = true;
        private class UwpContext
        {
            public string DeviceFamily { get; set; }
            public string DeviceManufacturer { get; set; }
            public string DeviceModel { get; set; }
            public string DeviceFriendlyName { get; set; }
            public string OsName { get; set; }
            public string OsVersion { get; set; }
            public string OsArchitecture { get; set; }


            public UwpContext()
            {

                DeviceFamily = AnalyticsInfo.VersionInfo.DeviceFamily;

                ulong version = ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
                ulong major = (version & 0xFFFF000000000000L) >> 48;
                ulong minor = (version & 0x0000FFFF00000000L) >> 32;
                ulong build = (version & 0x00000000FFFF0000L) >> 16;
                ulong revision = (version & 0x000000000000FFFFL);
                OsVersion = $"{major}.{minor}.{build}.{revision}";

                OsArchitecture = Package.Current.Id.Architecture.ToString();
                EasClientDeviceInformation deviceInfo = new EasClientDeviceInformation();
                OsName = deviceInfo.OperatingSystem;
                DeviceManufacturer = deviceInfo.SystemManufacturer;
                DeviceModel = deviceInfo.SystemProductName;
                DeviceFriendlyName = deviceInfo.FriendlyName;
            }
        }

        private SentryOptions _options;

        internal PlatformEventProcessor(SentryOptions options) => _options = options;

        public SentryEvent? Process(SentryEvent @event)
        {
            if (_uwpContextLoaded)
            {
                try
                {
                    var uwpContext = _uwpContext.Value;
                    @event.Contexts.Device.Family = uwpContext.DeviceFamily;
                    @event.Contexts.Device.Manufacturer = uwpContext.DeviceManufacturer;
                    @event.Contexts.Device.Model = uwpContext.DeviceModel;
                    @event.Contexts.Device.Name = uwpContext.DeviceFriendlyName;
                    @event.Contexts.OperatingSystem.Name = uwpContext.OsName;
                    @event.Contexts.OperatingSystem.Version = uwpContext.OsVersion;
                }
                catch(Exception ex)
                {
                    _options.DiagnosticLogger?.LogError("Failed to add UwpPlatformEventProcessor into event.", ex);
                    //In case of any failure, this process function will be disabled to avoid throwing exceptions for future events.
                    _uwpContextLoaded = false;
                    _ = ex;
                }
            }
            else
            {
                _options.DiagnosticLogger.LogDebug("UwpPlatformEventProcessor disabled due to previous error.");
            }
            return @event;
        }

    }
}
#endif
