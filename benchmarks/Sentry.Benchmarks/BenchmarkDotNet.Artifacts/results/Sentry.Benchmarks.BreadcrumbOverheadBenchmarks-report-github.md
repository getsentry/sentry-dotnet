``` ini

BenchmarkDotNet=v0.12.1, OS=centos 7
Intel Xeon CPU E3-1245 V2 3.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.200
  [Host]     : .NET Core 2.1.13 (CoreCLR 4.6.28008.01, CoreFX 4.6.28008.01), X64 RyuJIT
  Job-RWBLMV : .NET Core 2.1.13 (CoreCLR 4.6.28008.01, CoreFX 4.6.28008.01), X64 RyuJIT
  Job-XYCPIR : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT


```
|                                                 Method |        Job |       Runtime | BreadcrumbsCount |         Mean |     Error |    StdDev | Ratio | RatioSD |   Gen 0 |   Gen 1 |  Gen 2 | Allocated |
|------------------------------------------------------- |----------- |-------------- |----------------- |-------------:|----------:|----------:|------:|--------:|--------:|--------:|-------:|----------:|
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** | **Job-RWBLMV** | **.NET Core 2.1** |                **1** |     **220.0 ns** |   **2.60 ns** |   **2.31 ns** |  **1.00** |    **0.00** |  **0.0818** |       **-** |      **-** |     **344 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; | Job-RWBLMV | .NET Core 2.1 |                1 |   2,298.0 ns |   9.07 ns |   7.57 ns | 10.46 |    0.09 |  0.2975 |  0.1450 | 0.0038 |    1592 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; | Job-RWBLMV | .NET Core 2.1 |                1 |     222.6 ns |   0.45 ns |   0.38 ns |  1.01 |    0.01 |  0.0818 |       - |      - |     344 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; | Job-RWBLMV | .NET Core 2.1 |                1 |   1,413.4 ns |  22.82 ns |  19.06 ns |  6.44 |    0.12 |  0.5646 |       - |      - |    2376 B |
|                                                        |            |               |                  |              |           |           |       |         |         |         |        |           |
|                        &#39;Disabled SDK: Add breadcrumbs&#39; | Job-XYCPIR | .NET Core 3.1 |                1 |     197.7 ns |   3.77 ns |   3.52 ns |  1.00 |    0.00 |  0.0801 |       - |      - |     336 B |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; | Job-XYCPIR | .NET Core 3.1 |                1 |     541.9 ns |   4.93 ns |   4.61 ns |  2.74 |    0.04 |  0.1469 |       - |      - |     616 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; | Job-XYCPIR | .NET Core 3.1 |                1 |     199.2 ns |   0.65 ns |   0.55 ns |  1.01 |    0.02 |  0.0801 |       - |      - |     336 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; | Job-XYCPIR | .NET Core 3.1 |                1 |   1,069.2 ns |   4.16 ns |   3.69 ns |  5.40 |    0.09 |  0.5169 |       - |      - |    2168 B |
|                                                        |            |               |                  |              |           |           |       |         |         |         |        |           |
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** | **Job-RWBLMV** | **.NET Core 2.1** |               **10** |   **2,200.0 ns** |  **33.93 ns** |  **39.08 ns** |  **1.00** |    **0.00** |  **0.8163** |       **-** |      **-** |    **3440 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; | Job-RWBLMV | .NET Core 2.1 |               10 |  22,358.4 ns | 143.53 ns | 119.86 ns | 10.15 |    0.19 |  2.9907 |  1.4648 | 0.0305 |   15920 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; | Job-RWBLMV | .NET Core 2.1 |               10 |   2,179.6 ns |  24.58 ns |  21.79 ns |  0.99 |    0.02 |  0.8163 |       - |      - |    3440 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; | Job-RWBLMV | .NET Core 2.1 |               10 |   6,601.4 ns |  43.83 ns |  34.22 ns |  2.99 |    0.06 |  1.9150 |       - |      - |    8064 B |
|                                                        |            |               |                  |              |           |           |       |         |         |         |        |           |
|                        &#39;Disabled SDK: Add breadcrumbs&#39; | Job-XYCPIR | .NET Core 3.1 |               10 |   1,934.2 ns |  10.46 ns |   9.79 ns |  1.00 |    0.00 |  0.8011 |       - |      - |    3360 B |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; | Job-XYCPIR | .NET Core 3.1 |               10 |   5,267.3 ns |  62.82 ns |  52.46 ns |  2.72 |    0.03 |  1.4725 |       - |      - |    6160 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; | Job-XYCPIR | .NET Core 3.1 |               10 |   1,949.6 ns |   7.73 ns |   6.85 ns |  1.01 |    0.01 |  0.8011 |       - |      - |    3360 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; | Job-XYCPIR | .NET Core 3.1 |               10 |   5,762.4 ns |  35.28 ns |  31.27 ns |  2.98 |    0.02 |  1.8387 |       - |      - |    7712 B |
|                                                        |            |               |                  |              |           |           |       |         |         |         |        |           |
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** | **Job-RWBLMV** | **.NET Core 2.1** |              **100** |  **21,706.7 ns** |  **79.07 ns** |  **61.73 ns** |  **1.00** |    **0.00** |  **8.1787** |       **-** |      **-** |   **34400 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; | Job-RWBLMV | .NET Core 2.1 |              100 | 223,122.8 ns | 869.48 ns | 813.31 ns | 10.28 |    0.03 | 30.0293 | 14.8926 | 0.2441 |  159200 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; | Job-RWBLMV | .NET Core 2.1 |              100 |  21,707.3 ns |  74.37 ns |  62.11 ns |  1.00 |    0.00 |  8.1787 |       - |      - |   34400 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; | Job-RWBLMV | .NET Core 2.1 |              100 |  60,605.0 ns | 291.27 ns | 243.22 ns |  2.79 |    0.01 | 17.0898 |       - |      - |   71792 B |
|                                                        |            |               |                  |              |           |           |       |         |         |         |        |           |
|                        &#39;Disabled SDK: Add breadcrumbs&#39; | Job-XYCPIR | .NET Core 3.1 |              100 |  19,087.7 ns |  58.97 ns |  52.28 ns |  1.00 |    0.00 |  8.0261 |       - |      - |   33600 B |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; | Job-XYCPIR | .NET Core 3.1 |              100 |  51,924.6 ns | 153.97 ns | 128.57 ns |  2.72 |    0.01 | 14.7095 |       - |      - |   61600 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; | Job-XYCPIR | .NET Core 3.1 |              100 |  19,293.8 ns |  41.29 ns |  34.48 ns |  1.01 |    0.00 |  8.0261 |       - |      - |   33600 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; | Job-XYCPIR | .NET Core 3.1 |              100 |  53,559.3 ns | 755.75 ns | 706.93 ns |  2.81 |    0.04 | 15.9302 |  0.0610 |      - |   66736 B |
