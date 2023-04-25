``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1555/22H2/2022Update/SunValley2)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK=7.0.203
  [Host]     : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT AVX2
  Job-ASBPLM : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT AVX2

Runtime=.NET 6.0  

```
|           Method | TxRuntimeMs |         Mean |      Error |    StdDev |      Gen0 |      Gen1 |   Allocated |
|----------------- |------------ |-------------:|-----------:|----------:|----------:|----------:|------------:|
| **WithoutProfiling** |          **25** |     **31.30 ms** |   **0.275 ms** |  **0.257 ms** |         **-** |         **-** |     **8.04 KB** |
|    WithProfiling |          25 |    174.74 ms |   3.483 ms |  8.991 ms |  750.0000 |  250.0000 | 32157.78 KB |
| **WithoutProfiling** |         **100** |    **109.18 ms** |   **1.539 ms** |  **1.365 ms** |         **-** |         **-** |    **10.58 KB** |
|    WithProfiling |         100 |    277.84 ms |   5.522 ms | 11.281 ms |  666.6667 |  333.3333 | 32680.32 KB |
| **WithoutProfiling** |        **1000** |  **1,082.21 ms** |  **21.231 ms** | **26.850 ms** |         **-** |         **-** |     **9.02 KB** |
|    WithProfiling |        1000 |  1,258.33 ms |  24.908 ms | 34.095 ms | 1000.0000 |         - | 27429.73 KB |
| **WithoutProfiling** |       **10000** | **10,169.43 ms** |  **16.097 ms** | **15.057 ms** |         **-** |         **-** |    **10.81 KB** |
|    WithProfiling |       10000 | 10,399.50 ms | 108.043 ms | 95.778 ms | 3000.0000 | 1000.0000 | 63737.54 KB |
