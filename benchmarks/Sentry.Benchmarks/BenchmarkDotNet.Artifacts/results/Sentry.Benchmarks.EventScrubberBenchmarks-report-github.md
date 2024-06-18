```

BenchmarkDotNet v0.13.12, macOS Sonoma 14.5 (23F79) [Darwin 23.5.0]
Apple M2 Max, 1 CPU, 12 logical and 12 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 6.0.22 (6.0.2223.42425), Arm64 RyuJIT AdvSIMD
  Job-BYFWEZ : .NET 6.0.22 (6.0.2223.42425), Arm64 RyuJIT AdvSIMD

InvocationCount=1  UnrollFactor=1  

```
| Method                     | Mean     | Error    | StdDev   | Allocated |
|--------------------------- |---------:|---------:|---------:|----------:|
| ScrubEvent_DefaultDenyList | 39.13 μs | 1.383 μs | 3.945 μs |   3.74 KB |
