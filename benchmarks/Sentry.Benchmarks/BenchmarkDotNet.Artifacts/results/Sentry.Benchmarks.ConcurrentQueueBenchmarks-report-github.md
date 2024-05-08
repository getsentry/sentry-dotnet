```

BenchmarkDotNet v0.13.12, macOS Sonoma 14.4.1 (23E224) [Darwin 23.4.0]
Apple M2 Max, 1 CPU, 12 logical and 12 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 6.0.22 (6.0.2223.42425), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 6.0.22 (6.0.2223.42425), Arm64 RyuJIT AdvSIMD


```
| Method                   | N    | Mean     | Error     | StdDev    | Gen0      | Gen1     | Gen2     | Allocated |
|------------------------- |----- |---------:|----------:|----------:|----------:|---------:|---------:|----------:|
| ConcurrentQueueLiteAsync | 1000 | 3.377 ms | 0.0782 ms | 0.2305 ms | 1054.6875 | 476.5625 |  31.2500 |   2.18 MB |
| ConcurrentQueueAsync     | 1000 | 3.574 ms | 0.0741 ms | 0.2172 ms | 1066.4063 | 519.5313 | 144.5313 |   2.21 MB |
