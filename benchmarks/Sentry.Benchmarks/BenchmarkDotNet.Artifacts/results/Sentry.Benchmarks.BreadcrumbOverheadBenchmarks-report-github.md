``` ini

BenchmarkDotNet=v0.11.1, OS=centos 7
Intel Xeon CPU E3-1245 V2 3.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.300
  [Host] : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT
  Core   : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                                 Method | BreadcrumbsCount |         Mean |        Error |       StdDev | Scaled | ScaledSD |   Gen 0 |   Gen 1 |  Gen 2 | Allocated |
|------------------------------------------------------- |----------------- |-------------:|-------------:|-------------:|-------:|---------:|--------:|--------:|-------:|----------:|
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** |                **1** |     **488.2 ns** |     **4.061 ns** |     **3.799 ns** |   **1.00** |     **0.00** |  **0.0677** |       **-** |      **-** |     **288 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; |                1 |   2,248.5 ns |    23.790 ns |    21.089 ns |   4.61 |     0.05 |  0.2480 |  0.1221 | 0.0038 |    1320 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; |                1 |     558.9 ns |     5.259 ns |     4.662 ns |   1.14 |     0.01 |  0.0677 |       - |      - |     288 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; |                1 |   1,559.4 ns |    11.097 ns |     9.837 ns |   3.19 |     0.03 |  0.4921 |       - |      - |    2072 B |
|                                                        |                  |              |              |              |        |          |         |         |        |           |
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** |               **10** |   **4,843.3 ns** |    **34.109 ns** |    **30.237 ns** |   **1.00** |     **0.00** |  **0.6790** |       **-** |      **-** |    **2880 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; |               10 |  22,352.6 ns |   140.069 ns |   124.167 ns |   4.62 |     0.04 |  2.5024 |  1.2207 | 0.0305 |   13200 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; |               10 |   4,873.5 ns |    44.437 ns |    39.392 ns |   1.01 |     0.01 |  0.6790 |       - |      - |    2880 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; |               10 |   8,789.9 ns |    73.532 ns |    65.185 ns |   1.81 |     0.02 |  1.2512 |       - |      - |    5312 B |
|                                                        |                  |              |              |              |        |          |         |         |        |           |
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** |              **100** |  **48,274.3 ns** |   **391.861 ns** |   **327.221 ns** |   **1.00** |     **0.00** |  **6.8359** |       **-** |      **-** |   **28800 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; |              100 | 234,847.7 ns | 1,751.352 ns | 1,462.458 ns |   4.87 |     0.04 | 24.9023 | 12.2070 | 0.2441 |  132000 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; |              100 |  48,537.5 ns |   276.534 ns |   258.670 ns |   1.01 |     0.01 |  6.8359 |       - |      - |   28800 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; |              100 |  85,206.9 ns |   333.575 ns |   295.705 ns |   1.77 |     0.01 | 10.6201 |       - |      - |   44560 B |
