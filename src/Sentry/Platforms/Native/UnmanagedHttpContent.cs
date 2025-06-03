using Sentry.Extensibility;

namespace Sentry.Native;

internal sealed class UnmanagedHttpContent : SerializableHttpContent
{
    private readonly IntPtr _content;
    private readonly int _length = 0;
    private readonly IDiagnosticLogger? _logger;

    public UnmanagedHttpContent(IntPtr content, int length, IDiagnosticLogger? logger)
    {
        _content = content;
        _length = length;
        _logger = logger;
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        try
        {
            UnmanagedMemoryStream unmanagedStream;
            unsafe
            {
                unmanagedStream = new UnmanagedMemoryStream((byte*)_content.ToPointer(), _length);
            }
            using (unmanagedStream)
            {
                await unmanagedStream.CopyToAsync(stream).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to serialize unmanaged content into the network stream");
            throw;
        }
    }

    protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        try
        {
            unsafe
            {
                using var unmanagedStream = new UnmanagedMemoryStream((byte*)_content.ToPointer(), _length);
                unmanagedStream.CopyTo(stream);
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to serialize unmanaged content into the network stream");
            throw;
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _length;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        C.sentry_free(_content);
        base.Dispose(disposing);
    }
}
