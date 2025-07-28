```

BenchmarkDotNet v0.13.12, macOS 15.5 (24F74) [Darwin 24.5.0]
Apple M3 Pro, 1 CPU, 12 logical and 12 physical cores
.NET SDK 9.0.301
  [Host]     : .NET 8.0.14 (8.0.1425.11118), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.14 (8.0.1425.11118), Arm64 RyuJIT AdvSIMD


```
| Method          | BatchCount | OperationsPerInvoke | Mean        | Error    | StdDev   | Gen0   | Allocated |
|---------------- |----------- |-------------------- |------------:|---------:|---------:|-------:|----------:|
| **EnqueueAndFlush** | **10**         | **100**                 |  **1,774.5 ns** |  **7.57 ns** |  **6.71 ns** | **0.6104** |      **5 KB** |
| **EnqueueAndFlush** | **10**         | **200**                 |  **3,468.5 ns** | **11.16 ns** | **10.44 ns** | **1.2207** |     **10 KB** |
| **EnqueueAndFlush** | **10**         | **1000**                | **17,259.7 ns** | **51.92 ns** | **46.02 ns** | **6.1035** |     **50 KB** |
| **EnqueueAndFlush** | **100**        | **100**                 |    **857.5 ns** |  **4.21 ns** |  **3.73 ns** | **0.1469** |    **1.2 KB** |
| **EnqueueAndFlush** | **100**        | **200**                 |  **1,681.4 ns** |  **1.74 ns** |  **1.63 ns** | **0.2937** |   **2.41 KB** |
| **EnqueueAndFlush** | **100**        | **1000**                |  **8,302.2 ns** | **12.00 ns** | **10.64 ns** | **1.4648** |  **12.03 KB** |
