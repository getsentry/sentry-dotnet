

// ReSharper disable once CheckNamespace
namespace Sentry;


/// <summary>
/// Extension for MarshalExceptionMode Enum
/// </summary>
public static class MarshalExceptionModeExtension
{
    /// <summary>
    /// Converts the ExceptionMode enum to ObjCRuntime.Runtime.MarshalManagedException
    /// </summary>
    /// <returns>ObjCRuntime.Runtime.MarshalManagedException equivallent of the exception mode</returns>
    public static ObjCRuntime.MarshalManagedExceptionMode ToObjC(this MarshalExceptionMode value)
    {
        switch (value)
        {
            case MarshalExceptionMode.Default:
                return ObjCRuntime.MarshalManagedExceptionMode.Default;
            case MarshalExceptionMode.UnwindNativeCode:
                return ObjCRuntime.MarshalManagedExceptionMode.UnwindNativeCode;
            case MarshalExceptionMode.Abort:
                return ObjCRuntime.MarshalManagedExceptionMode.Abort;
            default:
                return ObjCRuntime.MarshalManagedExceptionMode.Default;
        }
    }

}

/// <summary>
/// Exception mode referencing ObjCRuntime.Runtime.MarshalManagedException
/// </summary>
public enum MarshalExceptionMode
{
    /// <summary>
    /// The default varies by platform. It's always ThrowObjectiveCException in .NET. For legacy Xamarin projects, it's ThrowObjectiveCException if the GC is in cooperative mode (watchOS), and UnwindNativeCode otherwise (iOS / watchOS / macOS). The default may change in the future.
    /// </summary>
    Default,
    /// <summary>
    /// This is the previous (undefined) behavior. This isn't available when using the GC in cooperative mode (which is the only option on watchOS; thus, this isn't a valid option on watchOS), nor when using CoreCLR, but it's the default option for all other platforms in legacy Xamarin projects
    /// </summary>
    UnwindNativeCode,
    /// <summary>
    /// Convert the managed exception into an Objective-C exception and throw the Objective-C exception. This is the default in .NET and on watchOS in legacy Xamarin projects
    /// </summary>
    ThrowObjectiveCException,
    /// <summary>
    /// Abort the process
    /// </summary>
    Abort,
    /// <summary>
    /// Disables the exception interception, so it doesn't make sense to set this value in the event handler, but once the event is raised it's too late to disable it. In any case, if set, it will behave as UnwindNativeCode
    /// </summary>
    Disable,
    /// <summary>
    /// Skip MarshalExceptionMode setting
    /// </summary>
    None
}
public partial class SentryOptions
{

    /// <summary>
    /// The .NET SDK specific options for the IOS platform.
    /// </summary>
    public IOSOptions IOS { get; }

    /// <summary>
    /// The .NET SDK specific options for the IOS platform.
    /// </summary>

    public class IOSOptions
    {
        /// <summary>
        /// Gets or sets the exception mode.
        /// The default is <see cref="MarshalExceptionMode.UnwindNativeCode"/>
        /// </summary>
        /// <remarks>
        /// - Setting ExceptionMode to `None` will disable the switch of marshalling bahavior (recommended for NativeAOT)
        /// see https://learn.microsoft.com/en-us/previous-versions/xamarin/ios/platform/exception-marshaling#events
        /// </remarks>
        /// <example>
        /// ...
        /// options.IOS.ExceptionMode = ExceptionMode.None
        /// ...
        /// </example>
        ///
        public MarshalExceptionMode ExceptionMode { get; set; } = MarshalExceptionMode.UnwindNativeCode;

    }
}
