``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i7-3770 CPU 3.40GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.1.300
  [Host] : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT
  Core   : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                         Method | EventCount |        Mean |      Error |     StdDev |     Gen 0 |   Gen 1 |  Allocated |
|----------------------------------------------- |----------- |------------:|-----------:|-----------:|----------:|--------:|-----------:|
|    **&#39;CaptureException with duplicate detection&#39;** |          **1** |    **185.2 us** |   **3.326 us** |   **2.948 us** |   **18.5547** |       **-** |   **52.61 KB** |
| &#39;CaptureException without duplicate detection&#39; |          1 |    182.2 us |   3.796 us |   4.519 us |   18.5547 |       - |   52.56 KB |
|    **&#39;CaptureException with duplicate detection&#39;** |         **10** |  **1,831.8 us** |  **35.123 us** |  **32.854 us** |  **185.5469** |  **1.9531** |  **526.07 KB** |
| &#39;CaptureException without duplicate detection&#39; |         10 |  1,818.0 us |  30.139 us |  28.192 us |  185.5469 |       - |   525.6 KB |
|    **&#39;CaptureException with duplicate detection&#39;** |        **100** | **17,975.4 us** | **335.530 us** | **344.564 us** | **1843.7500** | **31.2500** | **5260.72 KB** |
| &#39;CaptureException without duplicate detection&#39; |        100 | 17,545.7 us | 342.960 us | 352.195 us | 1843.7500 |       - | 5255.98 KB |
