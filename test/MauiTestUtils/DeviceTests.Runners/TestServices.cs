#nullable enable
using System;
using Microsoft.Maui.TestUtils.DeviceTests.Runners.HeadlessRunner;

namespace Microsoft.Maui.TestUtils.DeviceTests.Runners
{
<<<<<<< HEAD
	public static class TestServices
	{
		static IServiceProvider? s_services = null;
=======
    private static IServiceProvider? s_services = null;
>>>>>>> chore/net8-devicetests

		public static IServiceProvider Services
		{
			get
			{
				if (s_services is null)
				{
#if __ANDROID__
<<<<<<< HEAD
					s_services = MauiTestInstrumentation.Current?.Services ?? IPlatformApplication.Current?.Services;
=======
                s_services = MauiTestInstrumentation.Current?.Services ?? MauiApplication.Current.Services;
>>>>>>> chore/net8-devicetests
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