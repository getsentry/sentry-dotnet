``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1702/22H2/2022Update/SunValley2)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK=7.0.302
  [Host]     : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT AVX2 DEBUG
  Job-XAKHAH : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT AVX2

Runtime=.NET 6.0  

```
|                          Method | runtimeMs | collect | rundown | provider |      n |          Mean |       Error |      StdDev |        Median | Ratio | RatioSD |      Gen0 |     Gen1 |     Gen2 |  Allocated | Alloc Ratio |
|-------------------------------- |---------- |-------- |-------- |--------- |------- |--------------:|------------:|------------:|--------------:|------:|--------:|----------:|---------:|---------:|-----------:|------------:|
|     **DiagnosticsSessionStartStop** |         **?** |       **?** |       **?** |        **?** |      **?** |     **12.764 ms** |   **0.2544 ms** |   **0.6289 ms** |     **12.750 ms** |     **?** |       **?** |         **-** |        **-** |        **-** |     **8080 B** |           **?** |
|  SampleProfilerSessionStartStop |         ? |       ? |       ? |        ? |      ? |    202.801 ms |   0.2305 ms |   0.2156 ms |    202.759 ms |     ? |       ? |  666.6667 | 333.3333 | 333.3333 | 10294405 B |           ? |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
|                     **Transaction** |        **25** |   **False** |       **?** |        **?** |      **?** |     **26.836 ms** |   **0.0554 ms** |   **0.0518 ms** |     **26.836 ms** |     **?** |       **?** |         **-** |        **-** |        **-** |   **209695 B** |           **?** |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
|                     **Transaction** |        **25** |    **True** |       **?** |        **?** |      **?** |  **1,869.938 ms** | **110.4774 ms** | **325.7451 ms** |  **1,988.836 ms** |     **?** |       **?** |         **-** |        **-** |        **-** |  **2300928 B** |           **?** |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
|                     **Transaction** |       **100** |   **False** |       **?** |        **?** |      **?** |    **109.096 ms** |   **0.2472 ms** |   **0.2313 ms** |    **109.037 ms** |     **?** |       **?** |         **-** |        **-** |        **-** |   **285309 B** |           **?** |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
|                     **Transaction** |       **100** |    **True** |       **?** |        **?** |      **?** |  **1,849.174 ms** | **118.8488 ms** | **348.5630 ms** |  **1,989.864 ms** |     **?** |       **?** |         **-** |        **-** |        **-** |  **4384000 B** |           **?** |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
|                     **Transaction** |      **1000** |   **False** |       **?** |        **?** |      **?** |  **1,006.442 ms** |   **0.7579 ms** |   **0.7089 ms** |  **1,006.476 ms** |     **?** |       **?** |         **-** |        **-** |        **-** |  **1841000 B** |           **?** |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
|                     **Transaction** |      **1000** |    **True** |       **?** |        **?** |      **?** |  **2,740.554 ms** | **147.6695 ms** | **435.4070 ms** |  **2,989.115 ms** |     **?** |       **?** |         **-** |        **-** |        **-** |  **7482952 B** |           **?** |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
|                     **Transaction** |     **10000** |   **False** |       **?** |        **?** |      **?** | **10,067.206 ms** |   **1.5642 ms** |   **1.4631 ms** | **10,066.989 ms** |     **?** |       **?** | **1000.0000** |        **-** |        **-** | **21200216 B** |           **?** |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
|                     **Transaction** |     **10000** |    **True** |       **?** |        **?** |      **?** | **11,874.396 ms** | **236.6765 ms** | **323.9655 ms** | **11,989.353 ms** |     **?** |       **?** | **1000.0000** |        **-** |        **-** | **24871080 B** |           **?** |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
| **DiagnosticsSessionStartCopyStop** |         **?** |       **?** |   **False** |      **all** |      **?** |     **98.983 ms** |   **4.0416 ms** |  **11.9167 ms** |    **106.218 ms** |     **?** |       **?** |         **-** |        **-** |        **-** |    **18041 B** |           **?** |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
| **DiagnosticsSessionStartCopyStop** |         **?** |       **?** |   **False** |  **runtime** |      **?** |     **12.333 ms** |   **1.9275 ms** |   **5.6225 ms** |     **11.957 ms** |     **?** |       **?** |         **-** |        **-** |        **-** |    **14052 B** |           **?** |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
| **DiagnosticsSessionStartCopyStop** |         **?** |       **?** |   **False** |   **sample** |      **?** |     **98.474 ms** |   **3.4511 ms** |  **10.1755 ms** |    **103.118 ms** |     **?** |       **?** |         **-** |        **-** |        **-** |    **12977 B** |           **?** |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
| **DiagnosticsSessionStartCopyStop** |         **?** |       **?** |   **False** |      **tpl** |      **?** |      **9.190 ms** |   **2.8708 ms** |   **8.2828 ms** |     **11.774 ms** |     **?** |       **?** |         **-** |        **-** |        **-** |    **26449 B** |           **?** |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
| **DiagnosticsSessionStartCopyStop** |         **?** |       **?** |    **True** |      **all** |      **?** |    **112.681 ms** |   **4.1611 ms** |  **12.2690 ms** |    **119.549 ms** |     **?** |       **?** |  **166.6667** | **166.6667** | **166.6667** |  **3296852 B** |           **?** |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
| **DiagnosticsSessionStartCopyStop** |         **?** |       **?** |    **True** |  **runtime** |      **?** |     **12.433 ms** |   **0.4167 ms** |   **1.2088 ms** |     **11.861 ms** |     **?** |       **?** |  **500.0000** | **500.0000** | **500.0000** |  **3269935 B** |           **?** |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
| **DiagnosticsSessionStartCopyStop** |         **?** |       **?** |    **True** |   **sample** |      **?** |    **107.295 ms** |   **4.8091 ms** |  **14.1797 ms** |    **110.104 ms** |     **?** |       **?** |  **166.6667** | **166.6667** | **166.6667** |  **3263068 B** |           **?** |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
| **DiagnosticsSessionStartCopyStop** |         **?** |       **?** |    **True** |      **tpl** |      **?** |     **27.430 ms** |   **2.6175 ms** |   **7.6768 ms** |     **27.496 ms** |     **?** |       **?** |  **133.3333** | **133.3333** | **133.3333** |  **3369689 B** |           **?** |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
|                      **DoHardWork** |         **?** |       **?** |       **?** |        **?** |  **10000** |      **6.077 ms** |   **0.0235 ms** |   **0.0220 ms** |      **6.080 ms** |  **1.00** |    **0.00** |         **-** |        **-** |        **-** |        **4 B** |        **1.00** |
|        DoHardWorkWhileProfiling |         ? |       ? |       ? |        ? |  10000 |      6.313 ms |   0.0245 ms |   0.0205 ms |      6.308 ms |  1.04 |    0.00 |         - |        - |        - |    51446 B |   12,861.50 |
|                                 |           |         |         |          |        |               |             |             |               |       |         |           |          |          |            |             |
|                      **DoHardWork** |         **?** |       **?** |       **?** |        **?** | **100000** |    **199.804 ms** |   **0.2485 ms** |   **0.2325 ms** |    **199.814 ms** |  **1.00** |    **0.00** |         **-** |        **-** |        **-** |     **1747 B** |        **1.00** |
|        DoHardWorkWhileProfiling |         ? |       ? |       ? |        ? | 100000 |    210.870 ms |   4.0807 ms |   4.5357 ms |    209.802 ms |  1.06 |    0.02 |         - |        - |        - |  2221048 B |    1,271.35 |