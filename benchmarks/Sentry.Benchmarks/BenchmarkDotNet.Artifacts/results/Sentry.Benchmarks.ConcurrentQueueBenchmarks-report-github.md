```

BenchmarkDotNet v0.13.12, macOS Sonoma 14.4.1 (23E224) [Darwin 23.4.0]
Apple M2 Max, 1 CPU, 12 logical and 12 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 6.0.22 (6.0.2223.42425), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 6.0.22 (6.0.2223.42425), Arm64 RyuJIT AdvSIMD


```
| Method                   | N    | Mean     | Error     | StdDev    | Gen0      | Gen1     | Gen2     | Allocated |
|------------------------- |----- |---------:|----------:|----------:|----------:|---------:|---------:|----------:|
| ConcurrentQueueLiteAsync | 1000 | 3.355 ms | 0.0888 ms | 0.2561 ms | 1058.5938 | 484.3750 |  23.4375 |   2.18 MB |
| ConcurrentQueueAsync     | 1000 | 5.127 ms | 0.1018 ms | 0.2574 ms | 1023.4375 | 484.3750 | 117.1875 |   2.21 MB |
