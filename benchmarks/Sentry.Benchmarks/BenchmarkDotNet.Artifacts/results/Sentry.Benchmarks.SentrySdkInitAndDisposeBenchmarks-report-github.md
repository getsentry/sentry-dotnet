``` ini

BenchmarkDotNet=v0.12.1, OS=centos 7
Intel Xeon CPU E3-1245 V2 3.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.200
  [Host]     : .NET Core 2.1.13 (CoreCLR 4.6.28008.01, CoreFX 4.6.28008.01), X64 RyuJIT
  Job-RWBLMV : .NET Core 2.1.13 (CoreCLR 4.6.28008.01, CoreFX 4.6.28008.01), X64 RyuJIT
  Job-XYCPIR : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT


```
|                                                  Method |        Job |       Runtime |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|-------------------------------------------------------- |----------- |-------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|------:|----------:|
|           &#39;Init/Dispose: no DSN provided, disabled SDK&#39; | Job-RWBLMV | .NET Core 2.1 |  4.844 μs | 0.0165 μs | 0.0154 μs |  1.00 |    0.00 | 0.0992 |      - |     - |     440 B |
| &#39;Init/Dispose: DSN provided via parameter, enabled SDK&#39; | Job-RWBLMV | .NET Core 2.1 | 87.332 μs | 0.6607 μs | 0.5857 μs | 18.03 |    0.14 | 2.0752 | 0.2441 |     - |    8190 B |
|            &#39;Init/Dispose: DSN via env var, enabled SDK&#39; | Job-RWBLMV | .NET Core 2.1 | 89.453 μs | 1.6202 μs | 1.4363 μs | 18.47 |    0.28 | 2.0752 | 0.2441 |     - |    8191 B |
|                                                         |            |               |           |           |           |       |         |        |        |       |           |
|           &#39;Init/Dispose: no DSN provided, disabled SDK&#39; | Job-XYCPIR | .NET Core 3.1 |  3.044 μs | 0.0430 μs | 0.0402 μs |  1.00 |    0.00 | 0.0687 |      - |     - |     288 B |
| &#39;Init/Dispose: DSN provided via parameter, enabled SDK&#39; | Job-XYCPIR | .NET Core 3.1 | 87.798 μs | 0.7887 μs | 0.7378 μs | 28.84 |    0.49 | 2.0752 | 0.2441 |     - |    9073 B |
|            &#39;Init/Dispose: DSN via env var, enabled SDK&#39; | Job-XYCPIR | .NET Core 3.1 | 88.489 μs | 1.7688 μs | 1.7372 μs | 29.08 |    0.77 | 2.0752 | 0.2441 |     - |    9044 B |
