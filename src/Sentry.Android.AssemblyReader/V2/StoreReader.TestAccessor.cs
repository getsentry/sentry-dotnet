namespace Sentry.Android.AssemblyReader.V2;

internal partial class StoreReader
{
    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor
    {
        private readonly StoreReader _instance;

        internal TestAccessor(StoreReader instance)
        {
            _instance = instance;
        }

        internal bool IsSupported() => _instance.IsSupported();
    }
}
