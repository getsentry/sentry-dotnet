```

BenchmarkDotNet v0.13.12, macOS Sonoma 14.4.1 (23E224) [Darwin 23.4.0]
Apple M2 Max, 1 CPU, 12 logical and 12 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 6.0.22 (6.0.2223.42425), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 6.0.22 (6.0.2223.42425), Arm64 RyuJIT AdvSIMD


```
| Method                   | N    | Mean     | Error     | StdDev    | Gen0      | Gen1     | Gen2     | Allocated |
|------------------------- |----- |---------:|----------:|----------:|----------:|---------:|---------:|----------:|
| ConcurrentQueueLiteAsync | 1000 | 4.106 ms | 0.0962 ms | 0.2835 ms | 1074.2188 | 507.8125 |  23.4375 |   2.18 MB |
| ConcurrentQueueAsync     | 1000 | 5.073 ms | 0.1282 ms | 0.3781 ms | 1062.5000 | 515.6250 | 125.0000 |   2.21 MB |
