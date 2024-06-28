#nullable enable
using System;
using Microsoft.Maui.TestUtils.DeviceTests.Runners.HeadlessRunner;

namespace Microsoft.Maui.TestUtils.DeviceTests.Runners
{
    public static class TestServices

    /* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-ios)'
    Before:
            static IServiceProvider? s_services = null;
    After:
            private static IServiceProvider? s_services = null;
    */

    /* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-maccatalyst)'
    Before:
            static IServiceProvider? s_services = null;
    After:
            private static IServiceProvider? s_services = null;
    */
    {
        private static IServiceProvider? s_services = null;

        public static IServiceProvider Services
        {
            get
            {
                if (s_services is null)
                {
#if __ANDROID__
                    s_services = MauiTestInstrumentation.Current?.Services ?? IPlatformApplication.Current?.Services;
#elif __IOS__
					s_services = MauiTestApplicationDelegate.Current?.Services ?? IPlatformApplication.Current?.Services;
#elif WINDOWS
					s_services = IPlatformApplication.Current?.Services;
#endif
                }

                if (s_services is null)
                    throw new InvalidOperationException($"Test app could not find services.");

                return s_services;
            }
        }
    }
}
