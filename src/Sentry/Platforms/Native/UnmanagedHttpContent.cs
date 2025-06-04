using Sentry.Extensibility;

namespace Sentry.Native;

internal sealed class UnmanagedHttpContent : SerializableHttpContent
{
    private IntPtr _content;
    private readonly int _length = 0;
    private readonly IDiagnosticLogger? _logger;

    public UnmanagedHttpContent(IntPtr content, int length, IDiagnosticLogger? logger)
    {
        _content = content;
        _length = length;
        _logger = logger;
    }

    ~UnmanagedHttpContent()
    {
        Dispose(false);
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        ObjectDisposedException.ThrowIf(_content == IntPtr.Zero, this);
        try
        {
            using var unmanagedStream = CreateStream();
            await unmanagedStream.CopyToAsync(stream).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to serialize unmanaged content into the network stream");
            throw;
        }
    }

    protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_content == IntPtr.Zero, this);
        try
        {
            using var unmanagedStream = CreateStream();
            unmanagedStream.CopyTo(stream);
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
        IntPtr content = Interlocked.Exchange(ref _content, IntPtr.Zero);
        C.sentry_free(content);
        base.Dispose(disposing);
    }

    private unsafe UnmanagedMemoryStream CreateStream()
    {
        return new UnmanagedMemoryStream((byte*)_content.ToPointer(), _length);
    }
}
