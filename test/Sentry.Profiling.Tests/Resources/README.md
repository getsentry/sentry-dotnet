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
