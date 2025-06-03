using Sentry.Extensibility;

namespace Sentry.Native;

internal class UnmanagedHttpContent : SerializableHttpContent
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

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        try
        {
            unsafe
            {
                using var unmanagedStream = new UnmanagedMemoryStream((byte*)_content.ToPointer(), _length);
                return unmanagedStream.CopyToAsync(stream);
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
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        C.sentry_free(_content);
        base.Dispose(disposing);
    }
}
