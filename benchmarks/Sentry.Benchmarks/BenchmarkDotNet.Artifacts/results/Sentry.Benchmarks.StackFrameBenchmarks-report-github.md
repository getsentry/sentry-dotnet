```

BenchmarkDotNet v0.13.12, macOS Sonoma 14.4.1 (23E224) [Darwin 23.4.0]
Apple M2 Max, 1 CPU, 12 logical and 12 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 6.0.22 (6.0.2223.42425), Arm64 RyuJIT AdvSIMD
  Job-SWPLGJ : .NET 6.0.22 (6.0.2223.42425), Arm64 RyuJIT AdvSIMD

InvocationCount=1  UnrollFactor=1  

```
| Method            | N    | Mean     | Error    | StdDev   | Allocated |
|------------------ |----- |---------:|---------:|---------:|----------:|
| ConfigureAppFrame | 1000 | 62.92 μs | 1.249 μs | 3.064 μs |     976 B |
