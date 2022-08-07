namespace Sentry.Maui.Internal;

// This is a helper we register as a singleton on the service provider.
// It allows us to register other items that should be disposed when the service provider disposes.
// TODO: There might be something like this built-in to .NET already.  Investigate and replace if so.

internal class Disposer : IDisposable
{
    private readonly List<IDisposable> _disposables = new();

    public void Register(IDisposable disposable)
    {
        _disposables.Add(disposable);
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }
}
