``` ini

BenchmarkDotNet=v0.12.1, OS=centos 7
Intel Xeon CPU E3-1245 V2 3.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.200
  [Host]     : .NET Core 2.1.13 (CoreCLR 4.6.28008.01, CoreFX 4.6.28008.01), X64 RyuJIT
  Job-RWBLMV : .NET Core 2.1.13 (CoreCLR 4.6.28008.01, CoreFX 4.6.28008.01), X64 RyuJIT
  Job-XYCPIR : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT


```
|                                Method |        Job |       Runtime | RequestCount |         Mean |      Error |     StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------------- |----------- |-------------- |------------- |-------------:|-----------:|-----------:|------:|--------:|-------:|------:|------:|----------:|
|           **&#39;Without RetryAfterHandler&#39;** | **Job-RWBLMV** | **.NET Core 2.1** |            **1** |     **41.84 ns** |   **0.068 ns** |   **0.060 ns** |  **1.00** |    **0.00** |      **-** |     **-** |     **-** |         **-** |
|  &#39;With RetryAfterHandler OK response&#39; | Job-RWBLMV | .NET Core 2.1 |            1 |    113.91 ns |   0.228 ns |   0.202 ns |  2.72 |    0.00 | 0.0170 |     - |     - |      72 B |
| &#39;With RetryAfterHandler 429 response&#39; | Job-RWBLMV | .NET Core 2.1 |            1 |    150.25 ns |   0.297 ns |   0.263 ns |  3.59 |    0.01 | 0.0169 |     - |     - |      72 B |
|                                       |            |               |              |              |            |            |       |         |        |       |       |           |
|           &#39;Without RetryAfterHandler&#39; | Job-XYCPIR | .NET Core 3.1 |            1 |     32.90 ns |   0.670 ns |   0.658 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|  &#39;With RetryAfterHandler OK response&#39; | Job-XYCPIR | .NET Core 3.1 |            1 |    125.52 ns |   2.529 ns |   3.461 ns |  3.80 |    0.17 | 0.0172 |     - |     - |      72 B |
| &#39;With RetryAfterHandler 429 response&#39; | Job-XYCPIR | .NET Core 3.1 |            1 |    142.49 ns |   0.677 ns |   0.566 ns |  4.32 |    0.09 | 0.0172 |     - |     - |      72 B |
|                                       |            |               |              |              |            |            |       |         |        |       |       |           |
|           **&#39;Without RetryAfterHandler&#39;** | **Job-RWBLMV** | **.NET Core 2.1** |           **10** |    **161.58 ns** |   **0.301 ns** |   **0.251 ns** |  **1.00** |    **0.00** |      **-** |     **-** |     **-** |         **-** |
|  &#39;With RetryAfterHandler OK response&#39; | Job-RWBLMV | .NET Core 2.1 |           10 |    868.18 ns |   1.407 ns |   1.098 ns |  5.37 |    0.01 | 0.1707 |     - |     - |     720 B |
| &#39;With RetryAfterHandler 429 response&#39; | Job-RWBLMV | .NET Core 2.1 |           10 |  1,260.70 ns |   3.261 ns |   2.891 ns |  7.80 |    0.02 | 0.1698 |     - |     - |     720 B |
|                                       |            |               |              |              |            |            |       |         |        |       |       |           |
|           &#39;Without RetryAfterHandler&#39; | Job-XYCPIR | .NET Core 3.1 |           10 |    116.39 ns |   0.195 ns |   0.173 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|  &#39;With RetryAfterHandler OK response&#39; | Job-XYCPIR | .NET Core 3.1 |           10 |    862.60 ns |   2.806 ns |   2.487 ns |  7.41 |    0.02 | 0.1717 |     - |     - |     720 B |
| &#39;With RetryAfterHandler 429 response&#39; | Job-XYCPIR | .NET Core 3.1 |           10 |  1,272.28 ns |  16.703 ns |  15.624 ns | 10.93 |    0.13 | 0.1717 |     - |     - |     720 B |
|                                       |            |               |              |              |            |            |       |         |        |       |       |           |
|           **&#39;Without RetryAfterHandler&#39;** | **Job-RWBLMV** | **.NET Core 2.1** |          **100** |  **1,254.67 ns** |   **3.092 ns** |   **2.892 ns** |  **1.00** |    **0.00** |      **-** |     **-** |     **-** |         **-** |
|  &#39;With RetryAfterHandler OK response&#39; | Job-RWBLMV | .NET Core 2.1 |          100 |  8,342.30 ns |  11.229 ns |   8.767 ns |  6.65 |    0.02 | 1.7090 |     - |     - |    7200 B |
| &#39;With RetryAfterHandler 429 response&#39; | Job-RWBLMV | .NET Core 2.1 |          100 | 12,327.87 ns |  28.745 ns |  24.004 ns |  9.82 |    0.03 | 1.7090 |     - |     - |    7200 B |
|                                       |            |               |              |              |            |            |       |         |        |       |       |           |
|           &#39;Without RetryAfterHandler&#39; | Job-XYCPIR | .NET Core 3.1 |          100 |    888.38 ns |   3.315 ns |   2.939 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|  &#39;With RetryAfterHandler OK response&#39; | Job-XYCPIR | .NET Core 3.1 |          100 |  9,161.40 ns |  21.468 ns |  19.031 ns | 10.31 |    0.04 | 1.7090 |     - |     - |    7200 B |
| &#39;With RetryAfterHandler 429 response&#39; | Job-XYCPIR | .NET Core 3.1 |          100 | 12,450.14 ns | 119.714 ns | 111.981 ns | 14.01 |    0.12 | 1.7090 |     - |     - |    7200 B |
