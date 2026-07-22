```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.5.1 (25F80) [Darwin 25.5.0]
Apple M3 Pro, 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a


```
| Method                   | Mean     | Error     | StdDev    | Gen0   | Allocated |
|------------------------- |---------:|----------:|----------:|-------:|----------:|
| System_Lazy              | 6.909 ns | 0.0807 ns | 0.0715 ns | 0.0048 |      40 B |
| Sentry_Internal_LazyLite | 1.006 ns | 0.0102 ns | 0.0085 ns |      - |         - |
