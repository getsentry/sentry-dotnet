```

BenchmarkDotNet v0.13.12, macOS 15.5 (24F74) [Darwin 24.5.0]
Apple M3 Pro, 1 CPU, 12 logical and 12 physical cores
.NET SDK 9.0.301
  [Host]     : .NET 8.0.14 (8.0.1425.11118), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.14 (8.0.1425.11118), Arm64 RyuJIT AdvSIMD


```
| Method                   | BatchCount | OperationsPerInvoke | Mean       | Error     | StdDev    | Median     | Gen0   | Allocated |
|------------------------- |----------- |-------------------- |-----------:|----------:|----------:|-----------:|-------:|----------:|
| **EnqueueAndFlush**          | **10**         | **100**                 |   **3.087 μs** | **0.0305 μs** | **0.0286 μs** |   **3.077 μs** | **0.6104** |      **5 KB** |
| EnqueueAndFlush_Parallel | 10         | 100                 |  22.359 μs | 0.1047 μs | 0.0979 μs |  22.370 μs | 1.2207 |   9.98 KB |
| **EnqueueAndFlush**          | **10**         | **200**                 |   **6.192 μs** | **0.0263 μs** | **0.0246 μs** |   **6.188 μs** | **1.2207** |     **10 KB** |
| EnqueueAndFlush_Parallel | 10         | 200                 |  50.020 μs | 0.0814 μs | 0.0761 μs |  50.011 μs | 1.7090 |  13.92 KB |
| **EnqueueAndFlush**          | **10**         | **1000**                |  **29.180 μs** | **0.5809 μs** | **0.9044 μs** |  **28.735 μs** | **6.1035** |     **50 KB** |
| EnqueueAndFlush_Parallel | 10         | 1000                | 245.169 μs | 4.1653 μs | 3.8962 μs | 246.642 μs | 4.8828 |  43.35 KB |
| **EnqueueAndFlush**          | **100**        | **100**                 |   **2.235 μs** | **0.0441 μs** | **0.1014 μs** |   **2.262 μs** | **0.1450** |    **1.2 KB** |
| EnqueueAndFlush_Parallel | 100        | 100                 |  22.153 μs | 0.4426 μs | 0.9141 μs |  22.353 μs | 0.7019 |   5.86 KB |
| **EnqueueAndFlush**          | **100**        | **200**                 |   **4.712 μs** | **0.0878 μs** | **0.0821 μs** |   **4.678 μs** | **0.2899** |   **2.41 KB** |
| EnqueueAndFlush_Parallel | 100        | 200                 |  52.853 μs | 1.0549 μs | 2.2020 μs |  53.331 μs | 0.9155 |   7.52 KB |
| **EnqueueAndFlush**          | **100**        | **1000**                |  **22.633 μs** | **0.4470 μs** | **0.4390 μs** |  **22.302 μs** | **1.4648** |  **12.03 KB** |
| EnqueueAndFlush_Parallel | 100        | 1000                | 337.335 μs | 3.8933 μs | 3.6418 μs | 337.324 μs | 2.4414 |  20.71 KB |
