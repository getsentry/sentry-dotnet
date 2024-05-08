```

BenchmarkDotNet v0.13.12, macOS Sonoma 14.4.1 (23E224) [Darwin 23.4.0]
Apple M2 Max, 1 CPU, 12 logical and 12 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 6.0.22 (6.0.2223.42425), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 6.0.22 (6.0.2223.42425), Arm64 RyuJIT AdvSIMD


```
| Method                   | N    | Mean     | Error     | StdDev    | Gen0      | Gen1     | Gen2     | Allocated |
|------------------------- |----- |---------:|----------:|----------:|----------:|---------:|---------:|----------:|
| ConcurrentQueueLiteAsync | 1000 | 3.370 ms | 0.0879 ms | 0.2493 ms | 1050.7813 | 464.8438 |  46.8750 |   2.18 MB |
| ConcurrentQueueAsync     | 1000 | 3.920 ms | 0.0749 ms | 0.1597 ms | 1070.3125 | 523.4375 | 136.7188 |   2.21 MB |
