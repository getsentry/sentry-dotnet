``` ini

BenchmarkDotNet=v0.11.0, OS=Windows 10.0.17134.228 (1803/April2018Update/Redstone4)
Intel Core i7-7920HQ CPU 3.10GHz (Max: 3.00GHz) (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.1.301
  [Host] : .NET Core 2.1.3 (CoreCLR 4.6.26725.06, CoreFX 4.6.26725.05), 64bit RyuJIT
  Core   : .NET Core 2.1.3 (CoreCLR 4.6.26725.06, CoreFX 4.6.26725.05), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                         Method | EventCount |        Mean |      Error |     StdDev |     Gen 0 |  Allocated |
|----------------------------------------------- |----------- |------------:|-----------:|-----------:|----------:|-----------:|
|    **&#39;CaptureException with duplicate detection&#39;** |          **1** |    **125.2 us** |  **0.2474 us** |  **0.2314 us** |   **19.2871** |   **55.18 KB** |
| &#39;CaptureException without duplicate detection&#39; |          1 |    126.0 us |  2.4510 us |  2.9177 us |   19.0430 |   55.13 KB |
|    **&#39;CaptureException with duplicate detection&#39;** |         **10** |  **1,250.5 us** |  **2.6356 us** |  **2.3364 us** |  **191.4063** |  **551.79 KB** |
| &#39;CaptureException without duplicate detection&#39; |         10 |  1,245.8 us |  4.3792 us |  3.8820 us |  191.4063 |   551.3 KB |
|    **&#39;CaptureException with duplicate detection&#39;** |        **100** | **12,472.1 us** | **30.2181 us** | **28.2660 us** | **1906.2500** | **5517.89 KB** |
| &#39;CaptureException without duplicate detection&#39; |        100 | 12,278.8 us | 39.5281 us | 36.9746 us | 1921.8750 | 5513.03 KB |
