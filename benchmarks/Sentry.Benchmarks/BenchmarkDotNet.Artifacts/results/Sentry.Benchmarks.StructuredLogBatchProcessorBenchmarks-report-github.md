```

BenchmarkDotNet v0.13.12, macOS 15.5 (24F74) [Darwin 24.5.0]
Apple M3 Pro, 1 CPU, 12 logical and 12 physical cores
.NET SDK 9.0.301
  [Host]     : .NET 8.0.14 (8.0.1425.11118), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.14 (8.0.1425.11118), Arm64 RyuJIT AdvSIMD


```
| Method          | BatchCount | OperationsPerInvoke | Mean      | Error     | StdDev    | Gen0   | Allocated |
|---------------- |----------- |-------------------- |----------:|----------:|----------:|-------:|----------:|
| **EnqueueAndFlush** | **10**         | **100**                 |  **2.209 μs** | **0.0221 μs** | **0.0196 μs** | **0.7095** |    **5.8 KB** |
| **EnqueueAndFlush** | **10**         | **200**                 |  **3.829 μs** | **0.0287 μs** | **0.0268 μs** | **1.3199** |   **10.8 KB** |
| **EnqueueAndFlush** | **10**         | **1000**                | **18.363 μs** | **0.2813 μs** | **0.2889 μs** | **6.1951** |   **50.8 KB** |
| **EnqueueAndFlush** | **100**        | **100**                 |  **1.021 μs** | **0.0133 μs** | **0.0118 μs** | **0.2441** |      **2 KB** |
| **EnqueueAndFlush** | **100**        | **200**                 |  **1.816 μs** | **0.0211 μs** | **0.0176 μs** | **0.3910** |    **3.2 KB** |
| **EnqueueAndFlush** | **100**        | **1000**                |  **8.762 μs** | **0.0761 μs** | **0.0675 μs** | **1.5564** |  **12.83 KB** |
