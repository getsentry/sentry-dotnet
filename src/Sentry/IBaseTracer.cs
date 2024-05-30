namespace Sentry;

internal interface IBaseTracer: ITraceContextInternal
{
    internal bool IsOtelInstrumenter { get; }
}
