using Sentry.Extensibility;

namespace Sentry.Desktop;

// https://github.com/getsentry/sentry-unity/blob/3eb6eca6ed270c5ec023bf75ee53c1ca00bb7c82/src/Sentry.Unity.Native/SentryNativeBridge.cs

/// <summary>
/// P/Invoke to `sentry-native` functions.
/// </summary>
/// <see href="https://github.com/getsentry/sentry-native"/>
internal static class SentryNativeBridge
{
    public static bool Init(SentryOptions options)
    {
        _isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        var cOptions = sentry_options_new();

        // Note: DSN is not null because options.IsValid() must have returned true for this to be called.
        sentry_options_set_dsn(cOptions, options.Dsn!);

        if (options.Release is not null)
        {
            options.DiagnosticLogger?.LogDebug("Setting Release: {0}", options.Release);
            sentry_options_set_release(cOptions, options.Release);
        }

        if (options.Environment is not null)
        {
            options.DiagnosticLogger?.LogDebug("Setting Environment: {0}", options.Environment);
            sentry_options_set_environment(cOptions, options.Environment);
        }

        options.DiagnosticLogger?.LogDebug("Setting Debug: {0}", options.Debug);
        sentry_options_set_debug(cOptions, options.Debug ? 1 : 0);

        if (options.SampleRate.HasValue)
        {
            options.DiagnosticLogger?.LogDebug("Setting Sample Rate: {0}", options.SampleRate.Value);
            sentry_options_set_sample_rate(cOptions, options.SampleRate.Value);
        }

        // Disabling the native in favor of the C# layer for now
        options.DiagnosticLogger?.LogDebug("Disabling native auto session tracking");
        sentry_options_set_auto_session_tracking(cOptions, 0);

        var dir = GetCacheDirectory(options);
        // Note: don't use RuntimeInformation.IsOSPlatform - it will report windows on WSL.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            options.DiagnosticLogger?.LogDebug("Setting CacheDirectoryPath on Windows: {0}", dir);
            sentry_options_set_database_pathw(cOptions, dir);
        }
        else
        {
            options.DiagnosticLogger?.LogDebug("Setting CacheDirectoryPath: {0}", dir);
            sentry_options_set_database_path(cOptions, dir);
        }

        if (options.DiagnosticLogger is null)
        {
            _logger?.LogDebug("Unsetting the current native logger");
            _logger = null;
        }
        else
        {
            options.DiagnosticLogger.LogDebug($"{(_logger is null ? "Setting a" : "Replacing the")} native logger");
            _logger = options.DiagnosticLogger;
            sentry_options_set_logger(cOptions, new sentry_logger_function_t(nativeLog), IntPtr.Zero);
        }

        options.DiagnosticLogger?.LogDebug("Initializing sentry native");
        return 0 == sentry_init(cOptions);
    }

    /// <summary>
    /// Closes the native SDK.
    /// </summary>
    public static void Close() => sentry_close();

    // Call after native init() to check if the application has crashed in the previous run and clear the status.
    // Because the file is removed, the result will change on subsequent calls so it must be cached for the current runtime.
    internal static bool HandleCrashedLastRun(SentryOptions options)
    {
        var result = sentry_get_crashed_last_run() == 1;
        sentry_clear_crashed_last_run();
        return result;
    }

    internal static string GetCacheDirectory(SentryOptions options)
    {
        if (options.CacheDirectoryPath is null)
        {
            // same as the default of sentry-native
            return Path.Combine(Directory.GetCurrentDirectory(), ".sentry-native");
        }
        else
        {
            return Path.Combine(options.CacheDirectoryPath, "SentryNative");
        }
    }

    // libsentry.so
    [DllImport("sentry")]
    private static extern IntPtr sentry_options_new();

    [DllImport("sentry")]
    private static extern void sentry_options_set_dsn(IntPtr options, string dsn);

    [DllImport("sentry")]
    private static extern void sentry_options_set_release(IntPtr options, string release);

    [DllImport("sentry")]
    private static extern void sentry_options_set_debug(IntPtr options, int debug);

    [DllImport("sentry")]
    private static extern void sentry_options_set_environment(IntPtr options, string environment);

    [DllImport("sentry")]
    private static extern void sentry_options_set_sample_rate(IntPtr options, double rate);

    [DllImport("sentry")]
    private static extern void sentry_options_set_database_path(IntPtr options, string path);

    [DllImport("sentry")]
    private static extern void sentry_options_set_database_pathw(IntPtr options, [MarshalAs(UnmanagedType.LPWStr)] string path);

    [DllImport("sentry")]
    private static extern void sentry_options_set_auto_session_tracking(IntPtr options, int debug);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
    private delegate void sentry_logger_function_t(int level, IntPtr message, IntPtr argsAddress, IntPtr userData);

    [DllImport("sentry")]
    private static extern void sentry_options_set_logger(IntPtr options, [MarshalAs(UnmanagedType.FunctionPtr)]sentry_logger_function_t logger, IntPtr userData);

    // The logger we should forward native messages to. This is referenced by nativeLog() which in turn for.
    private static IDiagnosticLogger? _logger;
    private static bool _isLinux = false;

    // This method is called from the C library and forwards incoming messages to the currently set _logger.
    // TODO: Tried replacing this with [MarshalAs(UnmanagedType.FunctionPtr)] on the target argument (above)
    // [MonoPInvokeCallback(typeof(sentry_logger_function_t))]
    private static void nativeLog(int cLevel, IntPtr format, IntPtr args, IntPtr userData)
    {
        try
        {
            nativeLogImpl(cLevel, format, args, userData);
        }
        catch
        {
            // never allow an exception back to native code - it would crash the app
        }
    }

    private static void nativeLogImpl(int cLevel, IntPtr format, IntPtr args, IntPtr userData)
    {
        var logger = _logger;
        if (logger is null || format == IntPtr.Zero || args == IntPtr.Zero)
        {
            return;
        }

        // see sentry.h: sentry_level_e
        var level = cLevel switch
        {
            -1 => SentryLevel.Debug,
            0 => SentryLevel.Info,
            1 => SentryLevel.Warning,
            2 => SentryLevel.Error,
            3 => SentryLevel.Fatal,
            _ => SentryLevel.Info,
        };

        if (!logger.IsEnabled(level))
        {
            return;
        }

        string? message = null;
        try
        {
            // We cannot access C var-arg (va_list) in c# thus we pass it back to vsnprintf to do the formatting.
            // For Linux, we must make a copy of the VaList to be able to pass it back...
            if (_isLinux)
            {
                var argsStruct = Marshal.PtrToStructure<VaListLinux64>(args);
                var formattedLength = 0;
                WithMarshalledStruct(argsStruct, argsPtr =>
                {
                    formattedLength = 1 + vsnprintf_linux(IntPtr.Zero, UIntPtr.Zero, format, argsPtr);
                });

                WithAllocatedPtr(formattedLength, buffer =>
                WithMarshalledStruct(argsStruct, argsPtr =>
                {
                    vsnprintf_linux(buffer, (UIntPtr)formattedLength, format, argsPtr);
                    message = Marshal.PtrToStringAnsi(buffer);
                }));
            }
            else
            {
                var formattedLength = 1 + vsnprintf_windows(IntPtr.Zero, UIntPtr.Zero, format, args);
                WithAllocatedPtr(formattedLength, buffer =>
                {
                    vsnprintf_windows(buffer, (UIntPtr)formattedLength, format, args);
                    message = Marshal.PtrToStringAnsi(buffer);
                });
            }
        }
        catch (Exception err)
        {
            logger.LogError("Exception in native log forwarder.", err);
        }

        if (message == null)
        {
            // try to at least print the unreplaced message pattern
            message = Marshal.PtrToStringAnsi(format);
        }

        if (message != null)
        {
            logger.Log(level, $"Native: {message}");
        }
    }

    [DllImport("msvcrt", EntryPoint = "vsnprintf")]
    private static extern int vsnprintf_windows(IntPtr buffer, UIntPtr bufferSize, IntPtr format, IntPtr args);

    [DllImport("libc", EntryPoint = "vsnprintf")]
    private static extern int vsnprintf_linux(IntPtr buffer, UIntPtr bufferSize, IntPtr format, IntPtr args);

    // https://stackoverflow.com/a/4958507/2386130
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct VaListLinux64
    {
        uint gp_offset;
        uint fp_offset;
        IntPtr overflow_arg_area;
        IntPtr reg_save_area;
    }

    private static void WithAllocatedPtr(int size, Action<IntPtr> action)
    {
        var ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);
            action(ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    private static void WithMarshalledStruct<T>(T structure, Action<IntPtr> action) where T : notnull =>
        WithAllocatedPtr(Marshal.SizeOf(structure), ptr =>
        {
            Marshal.StructureToPtr(structure, ptr, false);
            action(ptr);
        });

    [DllImport("sentry")]
    private static extern int sentry_init(IntPtr options);

    [DllImport("sentry")]
    private static extern int sentry_close();

    [DllImport("sentry")]
    private static extern int sentry_get_crashed_last_run();

    [DllImport("sentry")]
    private static extern int sentry_clear_crashed_last_run();
}
