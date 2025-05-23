```

BenchmarkDotNet v0.13.12, macOS 15.4.1 (24E263) [Darwin 24.4.0]
Apple M2 Max, 1 CPU, 12 logical and 12 physical cores
.NET SDK 9.0.203
  [Host]     : .NET 9.0.4 (9.0.425.16305), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 9.0.4 (9.0.425.16305), Arm64 RyuJIT AdvSIMD


```
| Method           | ResolveOptionsWithServiceProvider | Mean     | Error    | StdDev   | Median   | Gen0    | Gen1   | Allocated |
|----------------- |---------------------------------- |---------:|---------:|---------:|---------:|--------:|-------:|----------:|
| **&#39;Build MAUI App&#39;** | **Directly**                          | **494.3 μs** | **19.62 μs** | **53.38 μs** | **476.0 μs** | **12.6953** | **2.9297** | **105.13 KB** |
| **&#39;Build MAUI App&#39;** | **ServiceProvider**                   | **488.1 μs** |  **8.56 μs** | **12.55 μs** | **486.6 μs** | **15.6250** | **3.9063** | **129.52 KB** |
| **&#39;Build MAUI App&#39;** | **InvokeConfigOptions**               | **499.5 μs** |  **9.93 μs** | **22.21 μs** | **501.9 μs** | **12.6953** | **3.9063** | **110.23 KB** |
