``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i7-7920HQ CPU 3.10GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
Frequency=3023438 Hz, Resolution=330.7493 ns, Timer=TSC
.NET Core SDK=2.1.300
  [Host] : .NET Core 2.1.0 (CoreCLR 4.6.26515.07, CoreFX 4.6.26515.06), 64bit RyuJIT
  Core   : .NET Core 2.1.0 (CoreCLR 4.6.26515.07, CoreFX 4.6.26515.06), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                                 Method | BreadcrumbsCount |         Mean |        Error |       StdDev |       Median | Scaled | ScaledSD |   Gen 0 | Allocated |
|------------------------------------------------------- |----------------- |-------------:|-------------:|-------------:|-------------:|-------:|---------:|--------:|----------:|
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** |                **1** |     **413.2 ns** |    **10.563 ns** |     **9.880 ns** |     **409.3 ns** |   **1.00** |     **0.00** |  **0.0682** |     **288 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; |                1 |   1,245.5 ns |    23.919 ns |    25.593 ns |   1,257.6 ns |   3.02 |     0.09 |  0.2460 |    1036 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; |                1 |     397.1 ns |     1.975 ns |     1.847 ns |     396.9 ns |   0.96 |     0.02 |  0.0682 |     288 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; |                1 |     840.0 ns |     5.646 ns |     5.281 ns |     839.1 ns |   2.03 |     0.05 |  0.1850 |     776 B |
|                                                        |                  |              |              |              |              |        |          |         |           |
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** |               **10** |   **3,978.3 ns** |    **22.403 ns** |    **20.956 ns** |   **3,975.2 ns** |   **1.00** |     **0.00** |  **0.6790** |    **2880 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; |               10 |  11,851.3 ns |   103.263 ns |    96.592 ns |  11,837.9 ns |   2.98 |     0.03 |  2.4719 |   10365 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; |               10 |   3,844.1 ns |    18.216 ns |    17.039 ns |   3,846.5 ns |   0.97 |     0.01 |  0.6790 |    2880 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; |               10 |   7,391.2 ns |    60.393 ns |    53.537 ns |   7,408.6 ns |   1.86 |     0.02 |  1.3962 |    5864 B |
|                                                        |                  |              |              |              |              |        |          |         |           |
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** |              **100** |  **40,199.6 ns** |   **252.515 ns** |   **236.203 ns** |  **40,136.0 ns** |   **1.00** |     **0.00** |  **6.8359** |   **28800 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; |              100 | 116,416.1 ns |   567.962 ns |   474.274 ns | 116,342.3 ns |   2.90 |     0.02 | 24.6582 |  103650 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; |              100 |  38,864.2 ns |   157.822 ns |   147.627 ns |  38,902.4 ns |   0.97 |     0.01 |  6.8359 |   28800 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; |              100 |  91,455.6 ns | 2,551.667 ns | 7,197.022 ns |  87,488.3 ns |   2.28 |     0.18 | 16.8457 |   71048 B |
