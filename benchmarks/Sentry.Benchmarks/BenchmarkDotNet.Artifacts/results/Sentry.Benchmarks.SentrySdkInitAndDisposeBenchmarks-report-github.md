``` ini

BenchmarkDotNet=v0.12.1, OS=macOS Catalina 10.15.4 (19E287) [Darwin 19.4.0]
Intel Core i7-7920HQ CPU 3.10GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.201
  [Host]     : .NET Core 2.1.16 (CoreCLR 4.6.28516.03, CoreFX 4.6.28516.10), X64 RyuJIT
  Job-XZODAQ : .NET Core 2.1.16 (CoreCLR 4.6.28516.03, CoreFX 4.6.28516.10), X64 RyuJIT
  Job-NIJLIK : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT


```
|                                                  Method |        Job |       Runtime |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|-------------------------------------------------------- |----------- |-------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
|           &#39;Init/Dispose: no DSN provided, disabled SDK&#39; | Job-XZODAQ | .NET Core 2.1 |  3.334 μs | 0.0542 μs | 0.0507 μs |  1.00 |    0.00 | 0.0992 |      - |      - |     440 B |
| &#39;Init/Dispose: DSN provided via parameter, enabled SDK&#39; | Job-XZODAQ | .NET Core 2.1 | 80.370 μs | 1.7719 μs | 5.1124 μs | 24.06 |    1.77 | 2.1973 | 0.2441 |      - |    8441 B |
|            &#39;Init/Dispose: DSN via env var, enabled SDK&#39; | Job-XZODAQ | .NET Core 2.1 | 83.750 μs | 2.0778 μs | 5.9617 μs | 25.45 |    1.88 | 2.1973 | 0.2441 |      - |    8447 B |
|                                                         |            |               |           |           |           |       |         |        |        |        |           |
|           &#39;Init/Dispose: no DSN provided, disabled SDK&#39; | Job-NIJLIK | .NET Core 3.1 |  2.314 μs | 0.0438 μs | 0.0410 μs |  1.00 |    0.00 | 0.0687 |      - |      - |     288 B |
| &#39;Init/Dispose: DSN provided via parameter, enabled SDK&#39; | Job-NIJLIK | .NET Core 3.1 | 69.687 μs | 1.3701 μs | 2.3634 μs | 29.84 |    1.18 | 2.1973 | 0.2441 |      - |    9142 B |
|            &#39;Init/Dispose: DSN via env var, enabled SDK&#39; | Job-NIJLIK | .NET Core 3.1 | 64.952 μs | 1.2623 μs | 1.6852 μs | 28.19 |    0.85 | 2.3193 | 0.4883 | 0.1221 |    9157 B |
