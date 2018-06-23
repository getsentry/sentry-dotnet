``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i7-7920HQ CPU 3.10GHz (Kaby Lake), 1 CPU, 6 logical and 6 physical cores
.NET Core SDK=2.1.103
  [Host] : .NET Core 2.0.7 (CoreCLR 4.6.26328.01, CoreFX 4.6.26403.03), 64bit RyuJIT
  Core   : .NET Core 2.0.7 (CoreCLR 4.6.26328.01, CoreFX 4.6.26403.03), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                                 Method | BreadcrumbsCount |        Mean |        Error |       StdDev | Scaled | ScaledSD |   Gen 0 | Allocated |
|------------------------------------------------------- |----------------- |------------:|-------------:|-------------:|-------:|---------:|--------:|----------:|
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** |                **1** |    **333.0 ns** |     **6.468 ns** |     **6.353 ns** |   **1.00** |     **0.00** |  **0.0682** |     **288 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; |                1 |    891.0 ns |    17.531 ns |    20.870 ns |   2.68 |     0.08 |  0.2470 |    1036 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; |                1 |    330.6 ns |     5.863 ns |     5.484 ns |   0.99 |     0.02 |  0.0682 |     288 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; |                1 |    773.6 ns |    15.062 ns |    19.049 ns |   2.32 |     0.07 |  0.1898 |     800 B |
|                                                        |                  |             |              |              |        |          |         |           |
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** |               **10** |  **3,221.1 ns** |    **28.846 ns** |    **25.571 ns** |   **1.00** |     **0.00** |  **0.6828** |    **2880 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; |               10 |  8,644.8 ns |   107.741 ns |    95.510 ns |   2.68 |     0.04 |  2.4567 |   10365 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; |               10 |  3,343.1 ns |    62.000 ns |    57.995 ns |   1.04 |     0.02 |  0.6828 |    2880 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; |               10 |  5,907.2 ns |    67.357 ns |    59.710 ns |   1.83 |     0.02 |  1.4038 |    5888 B |
|                                                        |                  |             |              |              |        |          |         |           |
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** |              **100** | **32,310.1 ns** |   **451.781 ns** |   **422.596 ns** |   **1.00** |     **0.00** |  **6.8359** |   **28800 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; |              100 | 89,391.9 ns | 1,605.341 ns | 2,143.084 ns |   2.77 |     0.07 | 24.6582 |  103650 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; |              100 | 32,238.9 ns |   373.048 ns |   291.251 ns |   1.00 |     0.02 |  6.8359 |   28800 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; |              100 | 68,171.1 ns | 1,018.460 ns |   952.668 ns |   2.11 |     0.04 | 16.8457 |   71072 B |
