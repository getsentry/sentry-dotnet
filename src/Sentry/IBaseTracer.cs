namespace Sentry;

internal interface IBaseTracer
{
    internal bool IsOtelInstrumenter { get; }
}
