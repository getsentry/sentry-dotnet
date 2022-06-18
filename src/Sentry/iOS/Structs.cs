using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;
using Sentry;

namespace Sentry.iOS
{

	[Native]
	internal enum SentryLogLevel : long
	{
		None = 1,
		Error,
		Debug,
		Verbose
	}

	[Native]
	internal enum SentryLevel : ulong
	{
		None = 0,
		Debug = 1,
		Info = 2,
		Warning = 3,
		Error = 4,
		Fatal = 5
	}

	[Native]
	internal enum SentryError : long
	{
		UnknownError = -1,
		InvalidDsnError = 100,
		SentryCrashNotInstalledError = 101,
		InvalidCrashReportError = 102,
		CompressionError = 103,
		JsonConversionError = 104,
		CouldNotFindDirectory = 105,
		RequestError = 106,
		EventNotSent = 107
	}

	//static class CFunctions
	//{
	//	// extern NSError * _Nullable NSErrorFromSentryError (SentryError error, NSString * _Nonnull description) __attribute__((visibility("default")));
	//	[DllImport("__Internal")]
	//	[Verify(PlatformInvoke)]
	//	[return: NullAllowed]
	//	static extern NSError NSErrorFromSentryError(SentryError error, NSString description);
	//}

	[Native]
	internal enum SentrySessionStatus : ulong
	{
		Ok = 0,
		Exited = 1,
		Crashed = 2,
		Abnormal = 3
	}

}
