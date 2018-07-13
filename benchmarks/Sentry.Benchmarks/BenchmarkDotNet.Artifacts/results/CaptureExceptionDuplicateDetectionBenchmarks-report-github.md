``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i7-7920HQ CPU 3.10GHz (Kaby Lake), 1 CPU, 6 logical and 6 physical cores
.NET Core SDK=2.1.300
  [Host] : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT
  Core   : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                         Method | EventCount |        Mean |      Error |     StdDev |     Gen 0 |   Gen 1 |  Allocated |
|----------------------------------------------- |----------- |------------:|-----------:|-----------:|----------:|--------:|-----------:|
|    **&#39;CaptureException with duplicate detection&#39;** |          **1** |    **132.7 us** |   **2.124 us** |   **1.987 us** |   **18.3105** |       **-** |   **52.17 KB** |
| &#39;CaptureException without duplicate detection&#39; |          1 |    133.2 us |   2.634 us |   3.425 us |   18.3105 |       - |   52.12 KB |
|    **&#39;CaptureException with duplicate detection&#39;** |         **10** |  **1,270.8 us** |  **27.055 us** |  **25.307 us** |  **183.5938** |  **3.9063** |   **521.7 KB** |
| &#39;CaptureException without duplicate detection&#39; |         10 |  1,290.7 us |  25.367 us |  40.963 us |  183.5938 |       - |  521.22 KB |
|    **&#39;CaptureException with duplicate detection&#39;** |        **100** | **12,715.0 us** | **177.803 us** | **157.618 us** | **1843.7500** | **15.6250** | **5216.96 KB** |
| &#39;CaptureException without duplicate detection&#39; |        100 | 12,525.6 us | 175.819 us | 155.859 us | 1843.7500 |       - | 5212.23 KB |
