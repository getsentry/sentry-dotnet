```

BenchmarkDotNet v0.13.12, macOS Sonoma 14.5 (23F79) [Darwin 23.5.0]
Apple M2 Max, 1 CPU, 12 logical and 12 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 6.0.22 (6.0.2223.42425), Arm64 RyuJIT AdvSIMD
  Job-IGCIGQ : .NET 6.0.22 (6.0.2223.42425), Arm64 RyuJIT AdvSIMD

InvocationCount=10  IterationCount=10  LaunchCount=1  
UnrollFactor=1  WarmupCount=1  

```
| Method         | SpanCount | Mean      | Error     | StdDev   | Allocated |
|--------------- |---------- |----------:|----------:|---------:|----------:|
| **ConcurrentBag**  | **1**         |  **15.57 μs** |  **3.013 μs** | **1.793 μs** |  **12.69 KB** |
| SyncCollection | 1         |  14.71 μs |  2.396 μs | 1.426 μs |  12.02 KB |
| **ConcurrentBag**  | **10**        |  **29.02 μs** |  **3.721 μs** | **2.214 μs** |  **26.17 KB** |
| SyncCollection | 10        |  26.50 μs |  3.180 μs | 1.663 μs |  25.68 KB |
| **ConcurrentBag**  | **100**       | **157.62 μs** |  **9.686 μs** | **5.764 μs** | **150.19 KB** |
| SyncCollection | 100       | 137.18 μs | 11.170 μs | 6.647 μs | 149.33 KB |
