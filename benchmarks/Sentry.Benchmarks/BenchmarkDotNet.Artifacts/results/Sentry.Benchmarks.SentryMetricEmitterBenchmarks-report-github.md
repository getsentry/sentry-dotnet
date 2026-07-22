```

BenchmarkDotNet v0.13.12, macOS 26.4.1 (25E253) [Darwin 25.4.0]
Apple M3 Pro, 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.203
  [Host]     : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 9.0.8 (9.0.825.36511), Arm64 RyuJIT AdvSIMD


```
| Method                        | Mean      | Error    | StdDev   | Gen0   | Gen1   | Allocated |
|------------------------------ |----------:|---------:|---------:|-------:|-------:|----------:|
| EmitWithoutAttributes         |  99.82 ns | 1.894 ns | 1.945 ns | 0.0640 | 0.0001 |     536 B |
| EmitWithAttributes_Enumerable | 148.19 ns | 0.544 ns | 0.454 ns | 0.0851 |      - |     712 B |
| EmitWithAttributes_Span       | 127.01 ns | 0.300 ns | 0.266 ns | 0.0706 | 0.0002 |     592 B |
| EmitWithAttributes_TagList    | 134.64 ns | 1.935 ns | 1.715 ns | 0.0706 | 0.0002 |     592 B |
