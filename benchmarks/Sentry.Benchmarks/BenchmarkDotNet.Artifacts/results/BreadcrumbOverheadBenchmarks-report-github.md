``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i7-3770 CPU 3.40GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.1.300
  [Host] : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT
  Core   : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                                 Method | BreadcrumbsCount |         Mean |        Error |       StdDev | Scaled | ScaledSD |   Gen 0 | Allocated |
|------------------------------------------------------- |----------------- |-------------:|-------------:|-------------:|-------:|---------:|--------:|----------:|
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** |                **1** |     **489.8 ns** |     **9.417 ns** |     **9.670 ns** |   **1.00** |     **0.00** |  **0.0677** |     **288 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; |                1 |   1,523.9 ns |    28.684 ns |    26.831 ns |   3.11 |     0.08 |  0.2460 |    1036 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; |                1 |     501.8 ns |     9.475 ns |     8.863 ns |   1.02 |     0.03 |  0.0677 |     288 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; |                1 |   1,104.6 ns |    25.524 ns |    22.626 ns |   2.26 |     0.06 |  0.1907 |     808 B |
|                                                        |                  |              |              |              |        |          |         |           |
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** |               **10** |   **4,933.4 ns** |    **74.732 ns** |    **66.248 ns** |   **1.00** |     **0.00** |  **0.6790** |    **2880 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; |               10 |  15,087.3 ns |   294.386 ns |   302.313 ns |   3.06 |     0.07 |  2.4414 |   10365 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; |               10 |   4,888.2 ns |    46.510 ns |    43.505 ns |   0.99 |     0.02 |  0.6790 |    2880 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; |               10 |   9,743.4 ns |   167.792 ns |   156.953 ns |   1.98 |     0.04 |  1.4038 |    5896 B |
|                                                        |                  |              |              |              |        |          |         |           |
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** |              **100** |  **48,937.8 ns** |   **974.880 ns** | **1,043.111 ns** |   **1.00** |     **0.00** |  **6.8359** |   **28800 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; |              100 | 151,281.5 ns | 1,904.577 ns | 1,688.358 ns |   3.09 |     0.07 | 24.6582 |  103650 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; |              100 |  49,515.4 ns |   937.646 ns |   920.894 ns |   1.01 |     0.03 |  6.8359 |   28800 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; |              100 | 112,380.8 ns | 1,746.310 ns | 1,458.248 ns |   2.30 |     0.05 | 16.8457 |   71080 B |
