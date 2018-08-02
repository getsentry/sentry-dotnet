``` ini

BenchmarkDotNet=v0.11.0, OS=Windows 10.0.17134.165 (1803/April2018Update/Redstone4)
Intel Core i7-7920HQ CPU 3.10GHz (Kaby Lake), 1 CPU, 6 logical and 6 physical cores
.NET Core SDK=2.1.300
  [Host] : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT
  Core   : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                Method | RequestCount |         Mean |      Error |       StdDev |       Median | Scaled | ScaledSD |  Gen 0 | Allocated |
|-------------------------------------- |------------- |-------------:|-----------:|-------------:|-------------:|-------:|---------:|-------:|----------:|
|           **&#39;Without RetryAfterHandler&#39;** |            **1** |     **52.25 ns** |   **1.093 ns** |     **2.445 ns** |     **52.37 ns** |   **1.00** |     **0.00** |      **-** |       **0 B** |
|  &#39;With RetryAfterHandler OK response&#39; |            1 |    168.11 ns |   2.536 ns |     2.373 ns |    168.21 ns |   3.23 |     0.18 | 0.0169 |      72 B |
| &#39;With RetryAfterHandler 429 response&#39; |            1 |    222.51 ns |   4.479 ns |     9.737 ns |    221.59 ns |   4.27 |     0.30 | 0.0169 |      72 B |
|                                       |              |              |            |              |              |        |          |        |           |
|           **&#39;Without RetryAfterHandler&#39;** |           **10** |    **151.96 ns** |   **3.074 ns** |     **5.543 ns** |    **150.64 ns** |   **1.00** |     **0.00** |      **-** |       **0 B** |
|  &#39;With RetryAfterHandler OK response&#39; |           10 |  1,066.89 ns |  62.233 ns |   183.496 ns |  1,115.96 ns |   7.03 |     1.23 | 0.1707 |     720 B |
| &#39;With RetryAfterHandler 429 response&#39; |           10 |  1,809.01 ns |  79.689 ns |   234.966 ns |  1,871.32 ns |  11.92 |     1.60 | 0.1698 |     720 B |
|                                       |              |              |            |              |              |        |          |        |           |
|           **&#39;Without RetryAfterHandler&#39;** |          **100** |  **1,708.44 ns** | **108.375 ns** |   **319.546 ns** |  **1,836.91 ns** |   **1.00** |     **0.00** |      **-** |       **0 B** |
|  &#39;With RetryAfterHandler OK response&#39; |          100 | 12,099.65 ns | 385.940 ns | 1,131.896 ns | 12,298.74 ns |   7.39 |     1.78 | 1.7090 |    7200 B |
| &#39;With RetryAfterHandler 429 response&#39; |          100 | 17,411.05 ns | 714.302 ns | 2,106.134 ns | 17,944.05 ns |  10.63 |     2.70 | 1.7090 |    7200 B |
