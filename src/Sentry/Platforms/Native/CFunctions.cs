using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Protocol;

namespace Sentry.Native;

// https://github.com/getsentry/sentry-unity/blob/3eb6eca6ed270c5ec023bf75ee53c1ca00bb7c82/src/Sentry.Unity/NativeUtils/CFunctions.cs

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

}
