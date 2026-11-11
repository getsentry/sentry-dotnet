using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Protocol;

namespace Sentry.Android;

internal static class NativeDebugImages
{
    private static Dictionary<long, DebugImage>? Cache;

    internal static Dictionary<long, DebugImage> Load(IDiagnosticLogger? logger)
    {
        if (Cache is { } cached)
        {
            return cached;
        }

        var images = LoadFromNdk(logger);
        if (images.Count > 0)
        {
            // The NDK caches its module list too, so reloading wouldn't pick up libraries loaded later.
            Cache = images;
        }
        return images;
    }

    private static Dictionary<long, DebugImage> LoadFromNdk(IDiagnosticLogger? logger)
    {
        logger?.LogDebug("Collecting a list of native debug images.");
        try
        {
            var modules = new global::IO.Sentry.Ndk.NativeModuleListLoader().LoadModuleList() ?? [];
            logger?.LogDebug("There are {0} native debug images, parsing the information.", modules.Length);
            return Convert(modules);
        }
        catch (Exception e)
        {
            logger?.LogWarning(e, "Error loading the list of debug images");
            return new();
        }
    }

    internal static Dictionary<long, DebugImage> Convert(IEnumerable<global::IO.Sentry.Ndk.DebugImage> modules)
    {
        var result = new Dictionary<long, DebugImage>();
        foreach (var module in modules)
        {
            if (module.ImageAddr is { Length: > 0 } imageAddr)
            {
                var imageAddress = imageAddr.ParseHexAsLong();
                result[imageAddress] = new DebugImage
                {
                    CodeFile = module.CodeFile,
                    ImageAddress = imageAddress,
                    ImageSize = module.ImageSize?.LongValue(),
                    DebugFile = module.DebugFile,
                    DebugId = module.DebugId,
                    CodeId = module.CodeId,
                    Type = module.Type,
                };
            }
        }
        return result;
    }
}
