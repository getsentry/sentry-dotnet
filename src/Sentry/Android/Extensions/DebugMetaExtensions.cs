namespace Sentry.Android.Extensions;

internal static class DebugMetaExtensions
{
    public static IList<DebugImage> ToDebugImages(this Java.Protocol.DebugMeta debugMeta) =>
        (debugMeta.Images?.Select(x => x.ToDebugImage()) ?? Enumerable.Empty<DebugImage>()).ToList();

    public static Java.Protocol.DebugMeta ToJavaDebugMeta(this IList<DebugImage> debugImages, SdkVersion sdkVersion)
    {
        // TODO: Investigate whether this is sufficient, or if we need to implement
        //       our own DebugMeta and SdkInfo classes to get this working correctly.

        var version = sdkVersion.Version is { } versionString
            ? System.Version.Parse(versionString)
            : null;

        return new Java.Protocol.DebugMeta
        {
            SdkInfo = new()
            {
                SdkName = sdkVersion.Name,
                VersionMajor = (JavaInteger?)version?.Major,
                VersionMinor = (JavaInteger?)version?.Minor,
                VersionPatchlevel = (JavaInteger?)version?.Build
            },
            Images = debugImages.Select(x => x.ToJavaDebugImage()).ToList()
        };
    }

    public static DebugImage ToDebugImage(this Java.Protocol.DebugImage debugImage) =>
        new()
        {
            Type = debugImage.Type,
            ImageAddress = debugImage.ImageAddr,
            ImageSize = debugImage.ImageSize?.LongValue(),
            DebugId = debugImage.DebugId,
            DebugFile = debugImage.DebugFile,
            CodeId = debugImage.CodeId,
            CodeFile = debugImage.CodeFile
        };

    public static Java.Protocol.DebugImage ToJavaDebugImage(this DebugImage debugImage) =>
        new()
        {
            Type = debugImage.Type,
            ImageAddr = debugImage.ImageAddress,
            ImageSize = (JavaLong?)debugImage.ImageSize,
            DebugId = debugImage.DebugId,
            DebugFile = debugImage.DebugFile,
            CodeId = debugImage.CodeId,
            CodeFile = debugImage.CodeFile
        };
}
