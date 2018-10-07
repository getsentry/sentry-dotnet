``` ini

BenchmarkDotNet=v0.11.0, OS=Windows 10.0.17134.228 (1803/April2018Update/Redstone4)
Intel Core i7-7920HQ CPU 3.10GHz (Max: 3.00GHz) (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.1.301
  [Host] : .NET Core 2.1.3 (CoreCLR 4.6.26725.06, CoreFX 4.6.26725.05), 64bit RyuJIT
  Core   : .NET Core 2.1.3 (CoreCLR 4.6.26725.06, CoreFX 4.6.26725.05), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                                 Method | BreadcrumbsCount |         Mean |         Error |        StdDev | Scaled | ScaledSD |   Gen 0 |   Gen 1 |  Gen 2 | Allocated |
|------------------------------------------------------- |----------------- |-------------:|--------------:|--------------:|-------:|---------:|--------:|--------:|-------:|----------:|
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** |                **1** |     **454.1 ns** |     **0.5344 ns** |     **0.4999 ns** |   **1.00** |     **0.00** |  **0.0682** |       **-** |      **-** |     **288 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; |                1 |   2,176.6 ns |     2.7491 ns |     2.2956 ns |   4.79 |     0.01 |  0.2480 |  0.1221 | 0.0038 |    1320 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; |                1 |     452.1 ns |     0.6559 ns |     0.5814 ns |   1.00 |     0.00 |  0.0682 |       - |      - |     288 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; |                1 |   1,292.8 ns |     3.4002 ns |     3.0142 ns |   2.85 |     0.01 |  0.4826 |       - |      - |    2032 B |
|                                                        |                  |              |               |               |        |          |         |         |        |           |
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** |               **10** |   **4,513.5 ns** |     **5.0130 ns** |     **4.4439 ns** |   **1.00** |     **0.00** |  **0.6790** |       **-** |      **-** |    **2880 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; |               10 |  21,660.4 ns |    56.7390 ns |    53.0737 ns |   4.80 |     0.01 |  2.5024 |  1.2207 | 0.0305 |   13200 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; |               10 |   4,423.4 ns |     9.1530 ns |     7.6431 ns |   0.98 |     0.00 |  0.6790 |       - |      - |    2880 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; |               10 |   7,012.3 ns |    15.4773 ns |    14.4775 ns |   1.55 |     0.00 |  1.2512 |       - |      - |    5272 B |
|                                                        |                  |              |               |               |        |          |         |         |        |           |
|                        **&#39;Disabled SDK: Add breadcrumbs&#39;** |              **100** |  **45,074.1 ns** |    **45.3122 ns** |    **42.3851 ns** |   **1.00** |     **0.00** |  **6.8359** |       **-** |      **-** |   **28800 B** |
|                         &#39;Enabled SDK: Add breadcrumbs&#39; |              100 | 214,683.2 ns | 1,213.2726 ns | 1,134.8958 ns |   4.76 |     0.02 | 24.9023 | 12.2070 | 0.2441 |  132000 B |
| &#39;Disabled SDK: Push scope, add breadcrumbs, pop scope&#39; |              100 |  43,996.0 ns |    58.8701 ns |    55.0672 ns |   0.98 |     0.00 |  6.8359 |       - |      - |   28800 B |
|  &#39;Enabled SDK: Push scope, add breadcrumbs, pop scope&#39; |              100 |  66,607.8 ns |   107.5510 ns |    89.8100 ns |   1.48 |     0.00 | 10.4980 |       - |      - |   44520 B |
