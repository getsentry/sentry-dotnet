namespace Sentry.Testing;

public static class StreamExtensions
{
    public static async Task FillWithRandomBytesAsync(this Stream stream, long length)
    {
        var remainingLength = length;
        var buffer = new byte[81920];
        var random = new Random();

        while (remainingLength > 0)
        {
            random.NextBytes(buffer);

            var bytesToCopy = (int)Math.Min(remainingLength, buffer.Length);
            await stream.WriteAsync(buffer, 0, bytesToCopy);

            remainingLength -= bytesToCopy;
        }
    }

    public static MemoryStream ToMemoryStream(this string source, Encoding encoding) =>
        new(encoding.GetBytes(source));

    public static MemoryStream ToMemoryStream(this string source) =>
        source.ToMemoryStream(Encoding.UTF8);
}
