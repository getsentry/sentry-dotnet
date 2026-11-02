# Info

File `*.nettrace` is an ETL capture produced by the following providers as used by `SamplingTransactionProfiler`

```cs
var providers = new[]
{
    new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, (long)ClrTraceEventParser.Keywords.Default),
    new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational),
    new EventPipeProvider("System.Threading.Tasks.TplEventSource", EventLevel.Informational, (long)TplEtwProviderTraceEventParser.Keywords.Default)
};
```

for the following transaction sample code in Aura.UI Gallery NetCore app:

```cs
// see https://github.com/PieroCastillo/Aura.UI/blob/1f9b12566b7272a8faa815821241d10fd5d52a92/samples/Aura.UI.Gallery.NetCore/Program.cs
public static int Main(string[] args)
{
    using (SentrySdk.Init(o =>
    {
        o.Dsn = DefaultDsn;
        o.Debug = true;
        o.TracesSampleRate = 1.0;
        o.AddProfilingIntegration();
        o.DiagnosticLogger = new FileAppenderDiagnosticLogger("C:/dev/Aura.UI/test.log", SentryLevel.Debug);
    }))
    {
        var tx = SentrySdk.StartTransaction("aura-gallery", "run");
        Task.Delay(50).ContinueWith(_ => tx.Finish());

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        return 0;
    }
}
```

Subsequently, the following code was used to produce the `.etlx` file we actually need to process events.

```cs
var etlFilePath = Path.Combine(_resourcesPath, "sample.nettrace");
var etlxFilePath = Path.ChangeExtension(etlFilePath, ".etlx");
TraceLog.CreateFromEventTraceLogFile(etlFilePath, etlxFilePath);
```

And for reference, you can create a JSON that can be displayed by [SpeedScope](https://speedscope.app):

```shell-script
dotnet-trace convert sample.nettrace --format Speedscope
```

## Regenerating `sample.etlx` after a perfview submodule bump

`sample.etlx` is committed, and its format version is tied to the TraceEvent build in
`modules/perfview`. A submodule bump can therefore leave it unreadable, which surfaces as:

```
FastSerialization.SerializationException : File format is version 74 App accepts formats >= 78.
```

To regenerate it, delete the file and run the tests once — `TraceLogProcessorTests` rebuilds it from
`sample.nettrace` when it is absent:

```shell-script
rm test/Sentry.Profiling.Tests/Resources/sample.etlx
dotnet test test/Sentry.Profiling.Tests --filter "FullyQualifiedName~TraceLogProcessorTests"
```

Then commit the regenerated file, along with any `*.verified.txt` snapshot changes it produces
(`pwsh ./scripts/accept-verifier-changes.ps1` — review the diff, module names in particular).

Do **not** leave it uncommitted and rely on regeneration at test time. The path is shared by every
target framework, and `dotnet test` runs one host per TFM, so they race on both this file and the
`sample.etlx.new` temp that TraceEvent writes alongside it.
