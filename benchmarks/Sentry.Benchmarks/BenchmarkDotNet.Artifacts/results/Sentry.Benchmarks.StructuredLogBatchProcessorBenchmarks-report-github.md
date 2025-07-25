```

BenchmarkDotNet v0.13.12, macOS 15.5 (24F74) [Darwin 24.5.0]
Apple M3 Pro, 1 CPU, 12 logical and 12 physical cores
.NET SDK 9.0.301
  [Host]     : .NET 8.0.14 (8.0.1425.11118), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.14 (8.0.1425.11118), Arm64 RyuJIT AdvSIMD


```
| Method          | BatchCount | OperationsPerInvoke | Mean        | Error     | StdDev    | Gen0   | Allocated |
|---------------- |----------- |-------------------- |------------:|----------:|----------:|-------:|----------:|
| **EnqueueAndFlush** | **10**         | **100**                 |  **1,874.9 ns** |  **33.18 ns** |  **31.04 ns** | **0.6104** |      **5 KB** |
| **EnqueueAndFlush** | **10**         | **200**                 |  **3,770.1 ns** |  **55.49 ns** |  **51.91 ns** | **1.2207** |     **10 KB** |
| **EnqueueAndFlush** | **10**         | **1000**                | **17,993.8 ns** | **359.21 ns** | **467.07 ns** | **6.1035** |     **50 KB** |
| **EnqueueAndFlush** | **100**        | **100**                 |    **809.3 ns** |  **15.05 ns** |  **15.45 ns** | **0.1469** |    **1.2 KB** |
| **EnqueueAndFlush** | **100**        | **200**                 |  **1,551.0 ns** |  **16.17 ns** |  **14.33 ns** | **0.2937** |   **2.41 KB** |
| **EnqueueAndFlush** | **100**        | **1000**                |  **7,782.0 ns** | **123.71 ns** | **109.67 ns** | **1.4648** |  **12.03 KB** |
