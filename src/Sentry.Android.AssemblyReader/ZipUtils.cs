namespace Sentry.Android.AssemblyReader;

internal static class ZipUtils
{
    internal static MemoryStream Extract(this ZipArchiveEntry zipEntry)
    {
        var memStream = new MemoryStream((int)zipEntry.Length);
        using var zipStream = zipEntry.Open();
        zipStream.CopyTo(memStream);
        memStream.Position = 0;
        return memStream;
    }
}
