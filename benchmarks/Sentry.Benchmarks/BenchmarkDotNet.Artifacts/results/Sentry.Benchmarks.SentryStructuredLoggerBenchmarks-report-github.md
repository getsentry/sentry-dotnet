```

BenchmarkDotNet v0.13.12, macOS 26.3.1 (a) (25D771280a) [Darwin 25.3.0]
Apple M3 Pro, 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD


```
| Method               | Mean     | Error   | StdDev  | Gen0   | Gen1   | Allocated |
|--------------------- |---------:|--------:|--------:|-------:|-------:|----------:|
| LogWithoutParameters | 102.3 ns | 1.28 ns | 1.19 ns | 0.0640 | 0.0001 |     536 B |
| LogWithParameters    | 248.5 ns | 4.86 ns | 5.40 ns | 0.1087 |      - |     912 B |
