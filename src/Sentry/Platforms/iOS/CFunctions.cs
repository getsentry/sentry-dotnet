using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Protocol;

namespace Sentry.iOS;

internal static class C
{
    internal static Dictionary<long, DebugImage> LoadDebugImages(IDiagnosticLogger? logger)
    {
        logger?.LogDebug("Collecting a list of native debug images.");
        var result = new Dictionary<long, DebugImage>();
        try
        {
            var cList = SentryCocoaHybridSdk.DebugImages;
            logger?.LogDebug("There are {0} native debug images, parsing the information.", cList.Length);
            foreach (var cItem in cList)
            {
                if (cItem.ImageAddress?.ParseHexAsLong() is { } imageAddress)
                {
                    result.Add(imageAddress, new DebugImage()
                    {
                        CodeFile = cItem.CodeFile,
                        ImageAddress = imageAddress,
                        ImageSize = cItem.ImageSize?.LongValue,
                        DebugId = cItem.DebugID,
                        Type = cItem.Type,
                    });
                }
            }
        }
        catch (Exception e)
        {
            logger?.LogWarning("Error loading the list of debug images", e);
        }
        return result;
    }
}
