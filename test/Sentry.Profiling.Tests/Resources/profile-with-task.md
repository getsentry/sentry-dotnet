# Info

File `prime-with-task.nettrace` is an ETL capture produced by the following providers as used by `SamplingTransactionProfiler`

```cs
var providers = new[]
{
    new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, (long)ClrTraceEventParser.Keywords.Default),
    new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational, (long)ClrTraceEventParser.Keywords.None)
};
```

for the following transaction code placed into `samples/Sentry.Samples.Console.Customized/Program.cs` built in
Release mode, net6.0, with all ".pdb" files manually removed before running.

```cs
var tx = SentrySdk.StartTransaction("app", "run");
var task = Task.Run(() => FindPrimeNumber(100000));
FindPrimeNumber(10000);
await task;
tx.Finish();


private static long FindPrimeNumber(int n)
{
    int count = 0;
    long a = 2;
    while (count < n)
    {
        long b = 2;
        int prime = 1;// to check if found a prime
        while (b * b <= a)
        {
            if (a % b == 0)
            {
                prime = 0;
                break;
            }
            b++;
        }
        if (prime > 0)
        {
            count++;
        }
        a++;
    }
    return (--a);
}
```

Subsequently, the following code was used to produce the `.etlx` file we actually need to process events.

```cs
var etlFilePath = Path.Combine(_resourcesPath, "profile-with-task.nettrace");
var etlxFilePath = Path.ChangeExtension(etlFilePath, ".etlx");
TraceLog.CreateFromEventTraceLogFile(etlFilePath, etlxFilePath);
```

And for reference, you can create a JSON that can be displayed by [SpeedScope](https://speedscope.app):

```shell-script
dotnet-trace convert profile-with-task.nettrace --format Speedscope
```