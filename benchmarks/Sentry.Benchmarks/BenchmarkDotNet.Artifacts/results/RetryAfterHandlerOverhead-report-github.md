``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i7-3770 CPU 3.40GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.1.300
  [Host] : .NET Core 2.1.2 (CoreCLR 4.6.26628.05, CoreFX 4.6.26629.01), 64bit RyuJIT
  Core   : .NET Core 2.1.2 (CoreCLR 4.6.26628.05, CoreFX 4.6.26629.01), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                Method | RequestCount |         Mean |       Error |      StdDev | Scaled | ScaledSD |  Gen 0 | Allocated |
|-------------------------------------- |------------- |-------------:|------------:|------------:|-------:|---------:|-------:|----------:|
|           **&#39;Without RetryAfterHandler&#39;** |            **1** |     **35.08 ns** |   **0.7278 ns** |   **0.8090 ns** |   **1.00** |     **0.00** |      **-** |       **0 B** |
|  &#39;With RetryAfterHandler OK response&#39; |            1 |    105.03 ns |   1.7523 ns |   1.6391 ns |   3.00 |     0.08 | 0.0170 |      72 B |
| &#39;With RetryAfterHandler 429 response&#39; |            1 |    143.22 ns |   2.7874 ns |   2.7376 ns |   4.08 |     0.12 | 0.0169 |      72 B |
|                                       |              |              |             |             |        |          |        |           |
|           **&#39;Without RetryAfterHandler&#39;** |           **10** |    **159.45 ns** |   **2.6417 ns** |   **2.3418 ns** |   **1.00** |     **0.00** |      **-** |       **0 B** |
|  &#39;With RetryAfterHandler OK response&#39; |           10 |    879.44 ns |   6.4467 ns |   5.3833 ns |   5.52 |     0.08 | 0.1707 |     720 B |
| &#39;With RetryAfterHandler 429 response&#39; |           10 |  1,211.91 ns |  19.3645 ns |  17.1661 ns |   7.60 |     0.15 | 0.1698 |     720 B |
|                                       |              |              |             |             |        |          |        |           |
|           **&#39;Without RetryAfterHandler&#39;** |          **100** |  **1,292.96 ns** |  **10.4123 ns** |   **9.2302 ns** |   **1.00** |     **0.00** |      **-** |       **0 B** |
|  &#39;With RetryAfterHandler OK response&#39; |          100 |  8,404.60 ns | 158.3983 ns | 155.5683 ns |   6.50 |     0.12 | 1.7090 |    7200 B |
| &#39;With RetryAfterHandler 429 response&#39; |          100 | 11,787.84 ns | 124.4315 ns | 103.9059 ns |   9.12 |     0.10 | 1.7090 |    7200 B |
