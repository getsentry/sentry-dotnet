using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Protocol;

namespace Sentry.Native;

/// <summary>
/// P/Invoke to `sentry-native` functions.
/// </summary>
/// <see href="https://github.com/getsentry/sentry-native"/>
internal static class C
{
    internal static void SetValueIfNotNull(sentry_value_t obj, string key, string? value)
    {
        if (value is not null)
        {
            _ = sentry_value_set_by_key(obj, key, sentry_value_new_string(value));
        }
    }

    internal static void SetValueIfNotNull(sentry_value_t obj, string key, int? value)
    {
        if (value.HasValue)
        {
            _ = sentry_value_set_by_key(obj, key, sentry_value_new_int32(value.Value));
        }
    }

    internal static void SetValueIfNotNull(sentry_value_t obj, string key, bool? value)
    {
        if (value.HasValue)
        {
            _ = sentry_value_set_by_key(obj, key, sentry_value_new_bool(value.Value ? 1 : 0));
        }
    }

    internal static void SetValueIfNotNull(sentry_value_t obj, string key, double? value)
    {
        if (value.HasValue)
        {
            _ = sentry_value_set_by_key(obj, key, sentry_value_new_double(value.Value));
        }
    }

    internal static sentry_value_t? GetValueOrNul(sentry_value_t obj, string key)
    {
        var cValue = sentry_value_get_by_key(obj, key);
        return sentry_value_is_null(cValue) == 0 ? cValue : null;
    }

    internal static string? GetValueString(sentry_value_t obj, string key)
    {
        if (GetValueOrNul(obj, key) is { } cValue)
        {
            var cString = sentry_value_as_string(cValue);
            if (cString != IntPtr.Zero)
            {
                return Marshal.PtrToStringAnsi(cString);
            }
        }
        return null;
    }

    internal static int? GetValueInt(sentry_value_t obj, string key)
    {
        if (GetValueOrNul(obj, key) is { } cValue)
        {
            return sentry_value_as_int32(cValue);
        }
        return null;
    }

    internal static double? GetValueDouble(sentry_value_t obj, string key)
    {
        if (GetValueOrNul(obj, key) is { } cValue)
        {
            return sentry_value_as_double(cValue);
        }
        return null;
    }

    public static bool Init(SentryOptions options)
    {
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
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            options.DiagnosticLogger?.LogDebug("Setting native CacheDirectoryPath on Windows: {0}", dir);
            sentry_options_set_database_pathw(cOptions, dir);
        }
        else
        {
            options.DiagnosticLogger?.LogDebug("Setting native CacheDirectoryPath: {0}", dir);
            sentry_options_set_database_path(cOptions, dir);
        }

        _isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        if (options.DiagnosticLogger is null)
        {
            _logger?.LogDebug("Unsetting the current native logger");
            _logger = null;
        }
        else
        {
            options.DiagnosticLogger.LogDebug($"{(_logger is null ? "Setting a" : "Replacing the")} native logger");
            _logger = options.DiagnosticLogger;
            unsafe
            {
                sentry_options_set_logger(cOptions, &nativeLog, IntPtr.Zero);
            }
        }

        options.DiagnosticLogger?.LogDebug("Initializing sentry native");
        return 0 == sentry_init(cOptions);
    }

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

    [DllImport("sentry-native")]
    internal static extern sentry_value_t sentry_value_new_object();

    [DllImport("sentry-native")]
    internal static extern sentry_value_t sentry_value_new_null();

    [DllImport("sentry-native")]
    internal static extern sentry_value_t sentry_value_new_bool(int value);

    [DllImport("sentry-native")]
    internal static extern sentry_value_t sentry_value_new_double(double value);

    [DllImport("sentry-native")]
    internal static extern sentry_value_t sentry_value_new_int32(int value);

    [DllImport("sentry-native")]
    internal static extern sentry_value_t sentry_value_new_string(string value);

    [DllImport("sentry-native")]
    internal static extern sentry_value_t sentry_value_new_breadcrumb(string? type, string? message);

    [DllImport("sentry-native")]
    internal static extern int sentry_value_set_by_key(sentry_value_t value, string k, sentry_value_t v);

    internal static bool IsNull(sentry_value_t value) => sentry_value_is_null(value) != 0;

    [DllImport("sentry-native")]
    internal static extern int sentry_value_is_null(sentry_value_t value);

    [DllImport("sentry-native")]
    internal static extern int sentry_value_as_int32(sentry_value_t value);

    [DllImport("sentry-native")]
    internal static extern double sentry_value_as_double(sentry_value_t value);

    [DllImport("sentry-native")]
    internal static extern IntPtr sentry_value_as_string(sentry_value_t value);

    [DllImport("sentry-native")]
    internal static extern UIntPtr sentry_value_get_length(sentry_value_t value);

    [DllImport("sentry-native")]
    internal static extern sentry_value_t sentry_value_get_by_index(sentry_value_t value, UIntPtr index);

    [DllImport("sentry-native")]
    internal static extern sentry_value_t sentry_value_get_by_key(sentry_value_t value, string key);

    [DllImport("sentry-native")]
    internal static extern void sentry_set_context(string key, sentry_value_t value);

    [DllImport("sentry-native")]
    internal static extern void sentry_add_breadcrumb(sentry_value_t breadcrumb);

    [DllImport("sentry-native")]
    internal static extern void sentry_set_tag(string key, string value);

    [DllImport("sentry-native")]
    internal static extern void sentry_remove_tag(string key);

    [DllImport("sentry-native")]
    internal static extern void sentry_set_user(sentry_value_t user);

    [DllImport("sentry-native")]
    internal static extern void sentry_remove_user();

    [DllImport("sentry-native")]
    internal static extern void sentry_set_extra(string key, sentry_value_t value);

    [DllImport("sentry-native")]
    internal static extern void sentry_remove_extra(string key);

    internal static Dictionary<long, DebugImage> LoadDebugImages(IDiagnosticLogger? logger)
    {
        // It only makes sense to load them once because they're cached on the native side anyway. We could force
        // native to reload the list by calling sentry_clear_modulecache() when a dynamic library is loaded, but
        // there's currently no way for us to know when that should happen.
        DebugImages ??= LoadDebugImagesOnce(logger);
        return DebugImages;
    }

    private static Dictionary<long, DebugImage>? DebugImages;

    private static Dictionary<long, DebugImage> LoadDebugImagesOnce(IDiagnosticLogger? logger)
    {
        logger?.LogDebug("Collecting a list of native debug images.");
        var result = new Dictionary<long, DebugImage>();
        try
        {
            var cList = sentry_get_modules_list();
            try
            {
                if (!IsNull(cList))
                {
                    var len = sentry_value_get_length(cList).ToUInt32();
                    logger?.LogDebug("There are {0} native debug images, parsing the information.", len);
                    for (uint i = 0; i < len; i++)
                    {
                        var cItem = sentry_value_get_by_index(cList, (UIntPtr)i);
                        if (!IsNull(cItem))
                        {
                            // See possible values present in `cItem` in the following files (or their latest versions)
                            // * https://github.com/getsentry/sentry-native/blob/8faa78298da68d68043f0c3bd694f756c0e95dfa/src/modulefinder/sentry_modulefinder_windows.c#L81
                            // * https://github.com/getsentry/sentry-native/blob/8faa78298da68d68043f0c3bd694f756c0e95dfa/src/modulefinder/sentry_modulefinder_windows.c#L24
                            // * https://github.com/getsentry/sentry-native/blob/c5c31e56d36bed37fa5422750a591f44502edb41/src/modulefinder/sentry_modulefinder_linux.c#L465
                            if (GetValueString(cItem, "image_addr") is { } imageAddr && imageAddr.Length > 0)
                            {
                                var imageAddress = imageAddr.ParseHexAsLong();
                                result.Add(imageAddress, new DebugImage()
                                {
                                    CodeFile = GetValueString(cItem, "code_file"),
                                    ImageAddress = imageAddress,
                                    ImageSize = GetValueInt(cItem, "image_size"),
                                    DebugFile = GetValueString(cItem, "debug_file"),
                                    DebugId = GetValueString(cItem, "debug_id"),
                                    CodeId = GetValueString(cItem, "code_id"),
                                    Type = GetValueString(cItem, "type"),
                                });
                            }
                        }
                    }
                }
            }
            finally
            {
                sentry_value_decref(cList);
            }
        }
        catch (Exception e)
        {
            logger?.LogWarning("Error loading the list of debug images", e);
        }
        return result;
    }

    // Returns a new reference to an immutable, frozen list.
    // The reference must be released with `sentry_value_decref`.
    [DllImport("sentry-native")]
    private static extern sentry_value_t sentry_get_modules_list();

    [DllImport("sentry-native")]
    internal static extern void sentry_value_decref(sentry_value_t value);

    // native union sentry_value_u/t
    [StructLayout(LayoutKind.Explicit)]
    internal struct sentry_value_t
    {
        [FieldOffset(0)]
        internal ulong _bits;
        [FieldOffset(0)]
        internal double _double;
    }

    [DllImport("sentry-native")]
    private static extern int sentry_init(IntPtr options);

    [DllImport("sentry-native")]
    private static extern int sentry_close();

    [DllImport("sentry-native")]
    private static extern int sentry_get_crashed_last_run();

    [DllImport("sentry-native")]
    private static extern int sentry_clear_crashed_last_run();

    [DllImport("sentry-native")]
    private static extern IntPtr sentry_options_new();

    [DllImport("sentry-native")]
    private static extern void sentry_options_set_dsn(IntPtr options, string dsn);

    [DllImport("sentry-native")]
    private static extern void sentry_options_set_release(IntPtr options, string release);

    [DllImport("sentry-native")]
    private static extern void sentry_options_set_debug(IntPtr options, int debug);

    [DllImport("sentry-native")]
    private static extern void sentry_options_set_environment(IntPtr options, string environment);

    [DllImport("sentry-native")]
    private static extern void sentry_options_set_sample_rate(IntPtr options, double rate);

    [DllImport("sentry-native")]
    private static extern void sentry_options_set_database_path(IntPtr options, string path);

    [DllImport("sentry-native")]
    private static extern void sentry_options_set_database_pathw(IntPtr options, [MarshalAs(UnmanagedType.LPWStr)] string path);

    [DllImport("sentry-native")]
    private static extern void sentry_options_set_auto_session_tracking(IntPtr options, int debug);

    [DllImport("sentry-native")]
    private static extern unsafe void sentry_options_set_logger(IntPtr options, delegate* unmanaged/*[Cdecl]*/<int, IntPtr, IntPtr, IntPtr, void> logger, IntPtr userData);

    // The logger we should forward native messages to. This is referenced by nativeLog() which in turn for.
    private static IDiagnosticLogger? _logger;
    private static bool _isLinux = false;

    // This method is called from the C library and forwards incoming messages to the currently set _logger.
    // [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]  //  error CS3016: Arrays as attribute arguments is not CLS-complian
    [UnmanagedCallersOnly]
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
                    formattedLength = 1 + vsnprintf_linux(IntPtr.Zero, UIntPtr.Zero, format, argsPtr)
                );

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
            logger.LogError(err, "Exception in native log forwarder.");
        }

        // If previous failed, try to at least print the unreplaced message pattern
        message ??= Marshal.PtrToStringAnsi(format);

        if (message != null)
        {
            logger.Log(level, $"[native] {message}");
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
        private uint _gp_offset;
        private uint _fp_offset;
        private IntPtr _overflow_arg_area;
        private IntPtr _reg_save_area;
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
}