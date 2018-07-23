``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i7-3770 CPU 3.40GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.1.300
  [Host] : .NET Core 2.1.2 (CoreCLR 4.6.26628.05, CoreFX 4.6.26629.01), 64bit RyuJIT
  Core   : .NET Core 2.1.2 (CoreCLR 4.6.26628.05, CoreFX 4.6.26629.01), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                Method | RequestCount |         Mean |       Error |      StdDev | Scaled | ScaledSD |   Gen 0 | Allocated |
|-------------------------------------- |------------- |-------------:|------------:|------------:|-------:|---------:|--------:|----------:|
|           **&#39;Without RetryAfterHandler&#39;** |            **1** |     **35.45 ns** |   **0.4068 ns** |   **0.3805 ns** |   **1.00** |     **0.00** |       **-** |       **0 B** |
|  &#39;With RetryAfterHandler OK response&#39; |            1 |    109.28 ns |   1.4007 ns |   1.1696 ns |   3.08 |     0.04 |  0.0170 |      72 B |
| &#39;With RetryAfterHandler 429 response&#39; |            1 |    319.34 ns |   6.2002 ns |   7.6143 ns |   9.01 |     0.23 |  0.1235 |     520 B |
|                                       |              |              |             |             |        |          |         |           |
|           **&#39;Without RetryAfterHandler&#39;** |           **10** |    **158.51 ns** |   **1.8310 ns** |   **1.6232 ns** |   **1.00** |     **0.00** |       **-** |       **0 B** |
|  &#39;With RetryAfterHandler OK response&#39; |           10 |    929.88 ns |  16.1702 ns |  13.5028 ns |   5.87 |     0.10 |  0.1707 |     720 B |
| &#39;With RetryAfterHandler 429 response&#39; |           10 |  2,964.67 ns |  68.6718 ns |  89.2927 ns |  18.71 |     0.58 |  1.2360 |    5200 B |
|                                       |              |              |             |             |        |          |         |           |
|           **&#39;Without RetryAfterHandler&#39;** |          **100** |  **1,329.99 ns** |  **26.2029 ns** |  **25.7347 ns** |   **1.00** |     **0.00** |       **-** |       **0 B** |
|  &#39;With RetryAfterHandler OK response&#39; |          100 |  9,026.42 ns | 178.0858 ns | 190.5498 ns |   6.79 |     0.19 |  1.7090 |    7200 B |
| &#39;With RetryAfterHandler 429 response&#39; |          100 | 29,843.93 ns | 587.5465 ns | 842.6417 ns |  22.45 |     0.75 | 12.3901 |   52000 B |
