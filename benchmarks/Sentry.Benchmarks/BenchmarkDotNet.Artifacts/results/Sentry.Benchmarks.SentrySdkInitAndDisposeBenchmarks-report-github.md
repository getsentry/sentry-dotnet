``` ini

BenchmarkDotNet=v0.11.1, OS=centos 7
Intel Xeon CPU E3-1245 V2 3.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.300
  [Host] : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT
  Core   : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                                  Method |       Mean |     Error |    StdDev | Scaled | ScaledSD |  Gen 0 |  Gen 1 | Allocated |
|-------------------------------------------------------- |-----------:|----------:|----------:|-------:|---------:|-------:|-------:|----------:|
|           &#39;Init/Dispose: no DSN provided, disabled SDK&#39; |   4.837 us | 0.0165 us | 0.0146 us |   1.00 |     0.00 | 0.0992 |      - |     440 B |
| &#39;Init/Dispose: DSN provided via parameter, enabled SDK&#39; | 117.354 us | 1.1127 us | 0.9291 us |  24.26 |     0.20 | 2.5635 | 0.2441 |    9595 B |
|            &#39;Init/Dispose: DSN via env var, enabled SDK&#39; | 118.580 us | 1.9411 us | 1.8157 us |  24.52 |     0.37 | 2.4414 | 0.2441 |    9637 B |
