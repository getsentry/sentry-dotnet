``` ini

BenchmarkDotNet=v0.11.1, OS=centos 7
Intel Xeon CPU E3-1245 V2 3.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.300
  [Host] : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT
  Core   : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                Method | RequestCount |         Mean |      Error |     StdDev | Scaled | ScaledSD |  Gen 0 | Allocated |
|-------------------------------------- |------------- |-------------:|-----------:|-----------:|-------:|---------:|-------:|----------:|
|           **&#39;Without RetryAfterHandler&#39;** |            **1** |     **41.60 ns** |  **0.1508 ns** |  **0.1411 ns** |   **1.00** |     **0.00** |      **-** |       **0 B** |
|  &#39;With RetryAfterHandler OK response&#39; |            1 |    112.01 ns |  0.6535 ns |  0.5457 ns |   2.69 |     0.02 | 0.0170 |      72 B |
| &#39;With RetryAfterHandler 429 response&#39; |            1 |    154.36 ns |  0.5502 ns |  0.5147 ns |   3.71 |     0.02 | 0.0169 |      72 B |
|                                       |              |              |            |            |        |          |        |           |
|           **&#39;Without RetryAfterHandler&#39;** |           **10** |    **163.36 ns** |  **0.5467 ns** |  **0.4847 ns** |   **1.00** |     **0.00** |      **-** |       **0 B** |
|  &#39;With RetryAfterHandler OK response&#39; |           10 |    873.54 ns |  3.1520 ns |  2.4608 ns |   5.35 |     0.02 | 0.1707 |     720 B |
| &#39;With RetryAfterHandler 429 response&#39; |           10 |  1,277.91 ns |  7.3075 ns |  6.8354 ns |   7.82 |     0.05 | 0.1698 |     720 B |
|                                       |              |              |            |            |        |          |        |           |
|           **&#39;Without RetryAfterHandler&#39;** |          **100** |  **1,266.63 ns** |  **5.7447 ns** |  **5.0926 ns** |   **1.00** |     **0.00** |      **-** |       **0 B** |
|  &#39;With RetryAfterHandler OK response&#39; |          100 |  8,599.00 ns | 65.6991 ns | 61.4550 ns |   6.79 |     0.05 | 1.7090 |    7200 B |
| &#39;With RetryAfterHandler 429 response&#39; |          100 | 12,392.59 ns | 53.3969 ns | 49.9475 ns |   9.78 |     0.05 | 1.7090 |    7200 B |
