```

BenchmarkDotNet v0.13.12, macOS 15.4.1 (24E263) [Darwin 24.4.0]
Apple M2 Max, 1 CPU, 12 logical and 12 physical cores
.NET SDK 9.0.203
  [Host]     : .NET 9.0.4 (9.0.425.16305), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 9.0.4 (9.0.425.16305), Arm64 RyuJIT AdvSIMD


```
| Method           | ResolveOptionsWithServiceProvider | Mean     | Error   | StdDev   | Gen0    | Gen1   | Allocated |
|----------------- |---------------------------------- |---------:|--------:|---------:|--------:|-------:|----------:|
| **&#39;Build MAUI App&#39;** | **Directly**                          | **470.8 μs** | **9.02 μs** |  **9.65 μs** | **11.7188** | **2.9297** |  **99.25 KB** |
| **&#39;Build MAUI App&#39;** | **ServiceProvider**                   | **473.6 μs** | **8.73 μs** | **13.85 μs** | **11.7188** | **2.9297** |  **98.66 KB** |
| **&#39;Build MAUI App&#39;** | **InvokeConfigOptions**               | **462.0 μs** | **8.84 μs** | **10.18 μs** | **11.7188** | **2.9297** |  **98.74 KB** |
