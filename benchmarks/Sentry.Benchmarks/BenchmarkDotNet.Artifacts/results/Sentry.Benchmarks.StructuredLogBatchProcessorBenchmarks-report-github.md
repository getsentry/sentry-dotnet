```

BenchmarkDotNet v0.13.12, macOS 15.6 (24G84) [Darwin 24.6.0]
Apple M3 Pro, 1 CPU, 12 logical and 12 physical cores
.NET SDK 9.0.301
  [Host]     : .NET 8.0.14 (8.0.1425.11118), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.14 (8.0.1425.11118), Arm64 RyuJIT AdvSIMD


```
| Method                   | BatchCount | OperationsPerInvoke | Mean         | Error       | StdDev      | Gen0   | Allocated |
|------------------------- |----------- |-------------------- |-------------:|------------:|------------:|-------:|----------:|
| **EnqueueAndFlush**          | **10**         | **100**                 |   **1,793.4 ns** |    **13.75 ns** |    **12.86 ns** | **0.6104** |      **5 KB** |
| EnqueueAndFlush_Parallel | 10         | 100                 |  18,550.8 ns |   368.24 ns |   889.34 ns | 1.1292 |   9.16 KB |
| **EnqueueAndFlush**          | **10**         | **200**                 |   **3,679.8 ns** |    **18.65 ns** |    **16.53 ns** | **1.2207** |     **10 KB** |
| EnqueueAndFlush_Parallel | 10         | 200                 |  41,246.4 ns |   508.07 ns |   475.25 ns | 1.7090 |  14.04 KB |
| **EnqueueAndFlush**          | **10**         | **1000**                |  **17,239.1 ns** |    **62.50 ns** |    **58.46 ns** | **6.1035** |     **50 KB** |
| EnqueueAndFlush_Parallel | 10         | 1000                | 192,059.3 ns |   956.92 ns |   895.11 ns | 4.3945 |  37.52 KB |
| **EnqueueAndFlush**          | **100**        | **100**                 |     **866.7 ns** |     **1.99 ns** |     **1.77 ns** | **0.1469** |    **1.2 KB** |
| EnqueueAndFlush_Parallel | 100        | 100                 |   6,714.8 ns |   100.75 ns |    94.24 ns | 0.5569 |   4.52 KB |
| **EnqueueAndFlush**          | **100**        | **200**                 |   **1,714.5 ns** |     **3.20 ns** |     **3.00 ns** | **0.2937** |   **2.41 KB** |
| EnqueueAndFlush_Parallel | 100        | 200                 |  43,842.8 ns |   860.74 ns | 1,718.99 ns | 0.9155 |   7.51 KB |
| **EnqueueAndFlush**          | **100**        | **1000**                |   **8,537.8 ns** |     **9.80 ns** |     **9.17 ns** | **1.4648** |  **12.03 KB** |
| EnqueueAndFlush_Parallel | 100        | 1000                | 313,421.4 ns | 6,159.27 ns | 6,846.01 ns | 1.9531 |  18.37 KB |
