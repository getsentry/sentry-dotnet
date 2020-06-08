``` ini

BenchmarkDotNet=v0.12.1, OS=centos 7
Intel Xeon CPU E3-1245 V2 3.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.200
  [Host]     : .NET Core 2.1.13 (CoreCLR 4.6.28008.01, CoreFX 4.6.28008.01), X64 RyuJIT
  Job-RWBLMV : .NET Core 2.1.13 (CoreCLR 4.6.28008.01, CoreFX 4.6.28008.01), X64 RyuJIT
  Job-XYCPIR : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT


```
|                                         Method |        Job |       Runtime | EventCount |        Mean |       Error |      StdDev |      Median |     Gen 0 |    Gen 1 |   Gen 2 |  Allocated |
|----------------------------------------------- |----------- |-------------- |----------- |------------:|------------:|------------:|------------:|----------:|---------:|--------:|-----------:|
|    **&#39;CaptureException with duplicate detection&#39;** | **Job-RWBLMV** | **.NET Core 2.1** |          **1** |    **165.3 μs** |     **3.27 μs** |     **3.50 μs** |    **164.9 μs** |   **15.6250** |   **6.3477** |  **0.4883** |   **61.23 KB** |
| &#39;CaptureException without duplicate detection&#39; | Job-RWBLMV | .NET Core 2.1 |          1 |    170.3 μs |     6.39 μs |    18.33 μs |    160.7 μs |   15.8691 |   6.3477 |  0.4883 |   61.19 KB |
|    &#39;CaptureException with duplicate detection&#39; | Job-XYCPIR | .NET Core 3.1 |          1 |    157.3 μs |    10.48 μs |    30.91 μs |    139.1 μs |   13.9160 |   5.3711 |  0.4883 |   63.97 KB |
| &#39;CaptureException without duplicate detection&#39; | Job-XYCPIR | .NET Core 3.1 |          1 |    148.4 μs |     3.76 μs |    10.10 μs |    144.8 μs |   13.4277 |   5.1270 |  0.2441 |   63.93 KB |
|    **&#39;CaptureException with duplicate detection&#39;** | **Job-RWBLMV** | **.NET Core 2.1** |         **10** |  **1,751.3 μs** |    **76.88 μs** |   **221.83 μs** |  **1,628.7 μs** |  **160.1563** |  **64.4531** |  **3.9063** |  **612.37 KB** |
| &#39;CaptureException without duplicate detection&#39; | Job-RWBLMV | .NET Core 2.1 |         10 |  1,940.5 μs |    88.17 μs |   259.97 μs |  1,886.7 μs |  164.0625 |  62.5000 |  3.9063 |   611.9 KB |
|    &#39;CaptureException with duplicate detection&#39; | Job-XYCPIR | .NET Core 3.1 |         10 |  1,386.2 μs |    17.79 μs |    21.85 μs |  1,379.3 μs |  134.7656 |  54.6875 |  3.9063 |  639.79 KB |
| &#39;CaptureException without duplicate detection&#39; | Job-XYCPIR | .NET Core 3.1 |         10 |  1,419.4 μs |    18.80 μs |    16.67 μs |  1,418.6 μs |  136.7188 |  46.8750 |  3.9063 |  639.38 KB |
|    **&#39;CaptureException with duplicate detection&#39;** | **Job-RWBLMV** | **.NET Core 2.1** |        **100** | **16,808.3 μs** |   **360.19 μs** |   **967.62 μs** | **16,444.1 μs** | **1531.2500** | **562.5000** | **62.5000** |    **6124 KB** |
| &#39;CaptureException without duplicate detection&#39; | Job-RWBLMV | .NET Core 2.1 |        100 | 18,759.1 μs | 1,136.66 μs | 3,351.47 μs | 17,182.5 μs | 1562.5000 | 562.5000 | 31.2500 | 6119.12 KB |
|    &#39;CaptureException with duplicate detection&#39; | Job-XYCPIR | .NET Core 3.1 |        100 | 14,013.6 μs |   347.32 μs |   956.61 μs | 13,592.7 μs | 1375.0000 | 546.8750 | 31.2500 | 6396.95 KB |
| &#39;CaptureException without duplicate detection&#39; | Job-XYCPIR | .NET Core 3.1 |        100 | 12,973.7 μs |   239.84 μs |   478.99 μs | 12,821.5 μs | 1390.6250 | 562.5000 | 31.2500 | 6393.08 KB |
