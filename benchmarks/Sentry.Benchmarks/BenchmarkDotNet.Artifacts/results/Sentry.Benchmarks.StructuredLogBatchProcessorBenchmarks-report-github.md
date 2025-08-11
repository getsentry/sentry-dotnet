```

BenchmarkDotNet v0.13.12, macOS 15.5 (24F74) [Darwin 24.5.0]
Apple M3 Pro, 1 CPU, 12 logical and 12 physical cores
.NET SDK 9.0.301
  [Host]     : .NET 8.0.14 (8.0.1425.11118), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.14 (8.0.1425.11118), Arm64 RyuJIT AdvSIMD


```
| Method                   | BatchCount | OperationsPerInvoke | Mean         | Error       | StdDev      | Median       | Gen0   | Allocated |
|------------------------- |----------- |-------------------- |-------------:|------------:|------------:|-------------:|-------:|----------:|
| **EnqueueAndFlush**          | **10**         | **100**                 |   **1,896.0 ns** |     **6.92 ns** |     **6.13 ns** |   **1,895.0 ns** | **0.6104** |      **5 KB** |
| EnqueueAndFlush_Parallel | 10         | 100                 |  18,372.7 ns |   362.04 ns |   731.34 ns |  18,432.1 ns | 1.1292 |   9.21 KB |
| **EnqueueAndFlush**          | **10**         | **200**                 |   **3,683.3 ns** |    **11.97 ns** |    **10.61 ns** |   **3,682.6 ns** | **1.2207** |     **10 KB** |
| EnqueueAndFlush_Parallel | 10         | 200                 |  41,416.1 ns |   814.20 ns | 1,360.35 ns |  40,730.6 ns | 1.7090 |  14.01 KB |
| **EnqueueAndFlush**          | **10**         | **1000**                |  **17,336.4 ns** |    **80.76 ns** |    **75.54 ns** |  **17,324.4 ns** | **6.1035** |     **50 KB** |
| EnqueueAndFlush_Parallel | 10         | 1000                | 188,962.0 ns | 1,311.63 ns | 1,226.90 ns | 189,209.6 ns | 4.3945 |   36.6 KB |
| **EnqueueAndFlush**          | **100**        | **100**                 |     **863.0 ns** |     **0.88 ns** |     **0.73 ns** |     **862.8 ns** | **0.1469** |    **1.2 KB** |
| EnqueueAndFlush_Parallel | 100        | 100                 |   6,898.0 ns |    58.37 ns |    54.60 ns |   6,898.0 ns | 0.5646 |   4.55 KB |
| **EnqueueAndFlush**          | **100**        | **200**                 |   **1,729.4 ns** |     **4.33 ns** |     **3.84 ns** |   **1,729.1 ns** | **0.2937** |   **2.41 KB** |
| EnqueueAndFlush_Parallel | 100        | 200                 |  34,233.3 ns |   286.48 ns |   267.97 ns |  34,238.6 ns | 0.9155 |   7.42 KB |
| **EnqueueAndFlush**          | **100**        | **1000**                |   **8,515.8 ns** |    **21.32 ns** |    **18.90 ns** |   **8,514.8 ns** | **1.4648** |  **12.03 KB** |
| EnqueueAndFlush_Parallel | 100        | 1000                | 317,992.9 ns | 2,076.68 ns | 1,942.53 ns | 318,190.1 ns | 1.9531 |  17.71 KB |
