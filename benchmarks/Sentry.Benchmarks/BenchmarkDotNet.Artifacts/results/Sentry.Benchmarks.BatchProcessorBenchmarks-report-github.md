```

BenchmarkDotNet v0.13.12, macOS 26.1 (25B78) [Darwin 25.1.0]
Apple M3 Pro, 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.14 (8.0.1425.11118), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.14 (8.0.1425.11118), Arm64 RyuJIT AdvSIMD


```
| Method                   | BatchCount | OperationsPerInvoke | Mean         | Error       | StdDev       | Median       | Gen0   | Allocated |
|------------------------- |----------- |-------------------- |-------------:|------------:|-------------:|-------------:|-------:|----------:|
| **EnqueueAndFlush**          | **10**         | **100**                 |   **1,896.9 ns** |     **9.94 ns** |      **8.81 ns** |   **1,894.2 ns** | **0.6104** |      **5 KB** |
| EnqueueAndFlush_Parallel | 10         | 100                 |  16,520.9 ns |   327.78 ns |    746.51 ns |  16,350.4 ns | 1.1292 |   9.29 KB |
| **EnqueueAndFlush**          | **10**         | **200**                 |   **4,085.5 ns** |    **80.03 ns** |     **74.86 ns** |   **4,087.1 ns** | **1.2207** |     **10 KB** |
| EnqueueAndFlush_Parallel | 10         | 200                 |  39,371.8 ns |   776.85 ns |  1,360.59 ns |  38,725.0 ns | 1.6479 |   13.6 KB |
| **EnqueueAndFlush**          | **10**         | **1000**                |  **18,829.3 ns** |   **182.18 ns** |    **142.24 ns** |  **18,836.4 ns** | **6.1035** |     **50 KB** |
| EnqueueAndFlush_Parallel | 10         | 1000                | 151,934.1 ns | 2,631.83 ns |  3,232.12 ns | 151,495.9 ns | 3.6621 |  31.31 KB |
| **EnqueueAndFlush**          | **100**        | **100**                 |     **864.9 ns** |     **2.16 ns** |      **1.68 ns** |     **865.0 ns** | **0.1469** |    **1.2 KB** |
| EnqueueAndFlush_Parallel | 100        | 100                 |   7,414.9 ns |    74.86 ns |     70.02 ns |   7,405.9 ns | 0.5722 |   4.61 KB |
| **EnqueueAndFlush**          | **100**        | **200**                 |   **1,836.9 ns** |    **15.28 ns** |     **12.76 ns** |   **1,834.9 ns** | **0.2937** |   **2.41 KB** |
| EnqueueAndFlush_Parallel | 100        | 200                 |  37,119.5 ns |   726.04 ns |  1,252.39 ns |  36,968.9 ns | 0.8545 |   7.27 KB |
| **EnqueueAndFlush**          | **100**        | **1000**                |   **8,567.2 ns** |    **84.25 ns** |     **74.68 ns** |   **8,547.4 ns** | **1.4648** |  **12.03 KB** |
| EnqueueAndFlush_Parallel | 100        | 1000                | 255,284.5 ns | 5,095.08 ns | 12,593.77 ns | 258,313.9 ns | 1.9531 |  19.02 KB |
