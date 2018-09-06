``` ini

BenchmarkDotNet=v0.11.0, OS=Windows 10.0.17134.228 (1803/April2018Update/Redstone4)
Intel Core i7-7920HQ CPU 3.10GHz (Max: 3.00GHz) (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.1.301
  [Host] : .NET Core 2.1.3 (CoreCLR 4.6.26725.06, CoreFX 4.6.26725.05), 64bit RyuJIT
  Core   : .NET Core 2.1.3 (CoreCLR 4.6.26725.06, CoreFX 4.6.26725.05), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                Method | RequestCount |         Mean |      Error |     StdDev | Scaled | ScaledSD |  Gen 0 | Allocated |
|-------------------------------------- |------------- |-------------:|-----------:|-----------:|-------:|---------:|-------:|----------:|
|           **&#39;Without RetryAfterHandler&#39;** |            **1** |     **33.97 ns** |  **0.1257 ns** |  **0.1176 ns** |   **1.00** |     **0.00** |      **-** |       **0 B** |
|  &#39;With RetryAfterHandler OK response&#39; |            1 |     99.59 ns |  0.1189 ns |  0.1112 ns |   2.93 |     0.01 | 0.0170 |      72 B |
| &#39;With RetryAfterHandler 429 response&#39; |            1 |    136.05 ns |  0.1600 ns |  0.1418 ns |   4.01 |     0.01 | 0.0169 |      72 B |
|                                       |              |              |            |            |        |          |        |           |
|           **&#39;Without RetryAfterHandler&#39;** |           **10** |    **158.18 ns** |  **0.1190 ns** |  **0.1113 ns** |   **1.00** |     **0.00** |      **-** |       **0 B** |
|  &#39;With RetryAfterHandler OK response&#39; |           10 |    769.97 ns |  0.8667 ns |  0.7683 ns |   4.87 |     0.01 | 0.1707 |     720 B |
| &#39;With RetryAfterHandler 429 response&#39; |           10 |  1,161.00 ns |  1.2244 ns |  1.1453 ns |   7.34 |     0.01 | 0.1698 |     720 B |
|                                       |              |              |            |            |        |          |        |           |
|           **&#39;Without RetryAfterHandler&#39;** |          **100** |  **1,219.51 ns** |  **7.5139 ns** |  **5.8663 ns** |   **1.00** |     **0.00** |      **-** |       **0 B** |
|  &#39;With RetryAfterHandler OK response&#39; |          100 |  7,340.84 ns |  8.3460 ns |  7.3985 ns |   6.02 |     0.03 | 1.7090 |    7200 B |
| &#39;With RetryAfterHandler 429 response&#39; |          100 | 11,351.22 ns | 13.6506 ns | 12.7687 ns |   9.31 |     0.04 | 1.7090 |    7200 B |
