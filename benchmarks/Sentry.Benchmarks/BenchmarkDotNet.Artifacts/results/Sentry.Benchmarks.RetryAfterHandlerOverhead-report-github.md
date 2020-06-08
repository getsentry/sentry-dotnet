``` ini

BenchmarkDotNet=v0.12.1, OS=macOS Catalina 10.15.4 (19E287) [Darwin 19.4.0]
Intel Core i7-7920HQ CPU 3.10GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.201
  [Host]     : .NET Core 2.1.16 (CoreCLR 4.6.28516.03, CoreFX 4.6.28516.10), X64 RyuJIT
  DefaultJob : .NET Core 2.1.16 (CoreCLR 4.6.28516.03, CoreFX 4.6.28516.10), X64 RyuJIT


```
|                                Method | RequestCount |         Mean |      Error |     StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------------- |------------- |-------------:|-----------:|-----------:|------:|--------:|-------:|------:|------:|----------:|
|           **&#39;Without RetryAfterHandler&#39;** |            **1** |     **38.58 ns** |   **0.797 ns** |   **1.309 ns** |  **1.00** |    **0.00** |      **-** |     **-** |     **-** |         **-** |
|  &#39;With RetryAfterHandler OK response&#39; |            1 |    103.64 ns |   2.026 ns |   2.333 ns |  2.69 |    0.12 | 0.0169 |     - |     - |      72 B |
| &#39;With RetryAfterHandler 429 response&#39; |            1 |    148.91 ns |   2.273 ns |   2.127 ns |  3.91 |    0.12 | 0.0169 |     - |     - |      72 B |
|                                       |              |              |            |            |       |         |        |       |       |           |
|           **&#39;Without RetryAfterHandler&#39;** |           **10** |    **152.08 ns** |   **1.755 ns** |   **1.466 ns** |  **1.00** |    **0.00** |      **-** |     **-** |     **-** |         **-** |
|  &#39;With RetryAfterHandler OK response&#39; |           10 |    796.14 ns |  14.680 ns |  27.930 ns |  5.30 |    0.21 | 0.1707 |     - |     - |     720 B |
| &#39;With RetryAfterHandler 429 response&#39; |           10 |  1,286.35 ns |  23.778 ns |  41.645 ns |  8.26 |    0.23 | 0.1698 |     - |     - |     720 B |
|                                       |              |              |            |            |       |         |        |       |       |           |
|           **&#39;Without RetryAfterHandler&#39;** |          **100** |  **1,173.64 ns** |  **16.637 ns** |  **15.563 ns** |  **1.00** |    **0.00** |      **-** |     **-** |     **-** |         **-** |
|  &#39;With RetryAfterHandler OK response&#39; |          100 |  7,388.06 ns | 147.623 ns | 206.947 ns |  6.39 |    0.14 | 1.7090 |     - |     - |    7200 B |
| &#39;With RetryAfterHandler 429 response&#39; |          100 | 12,329.64 ns | 242.122 ns | 226.481 ns | 10.51 |    0.20 | 1.7090 |     - |     - |    7200 B |
