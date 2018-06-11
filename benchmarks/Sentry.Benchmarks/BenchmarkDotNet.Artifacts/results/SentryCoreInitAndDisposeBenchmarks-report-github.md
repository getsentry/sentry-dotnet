``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i7-7920HQ CPU 3.10GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
Frequency=3023438 Hz, Resolution=330.7493 ns, Timer=TSC
.NET Core SDK=2.1.103
  [Host] : .NET Core 2.0.7 (CoreCLR 4.6.26328.01, CoreFX 4.6.26403.03), 64bit RyuJIT
  Core   : .NET Core 2.0.7 (CoreCLR 4.6.26328.01, CoreFX 4.6.26403.03), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                                  Method |      Mean |     Error |    StdDev | Scaled | ScaledSD |  Gen 0 |  Gen 1 | Allocated |
|-------------------------------------------------------- |----------:|----------:|----------:|-------:|---------:|-------:|-------:|----------:|
|           &#39;Init/Dispose: no DSN provided, disabled SDK&#39; |  4.327 us | 0.0845 us | 0.1184 us |   1.00 |     0.00 | 0.1678 |      - |     720 B |
| &#39;Init/Dispose: DSN provided via parameter, enabled SDK&#39; | 22.036 us | 0.4248 us | 0.6092 us |   5.10 |     0.19 | 1.2817 | 0.0610 |    5910 B |
|            &#39;Init/Dispose: DSN via env var, enabled SDK&#39; | 24.301 us | 0.5002 us | 1.3692 us |   5.62 |     0.35 | 1.2817 | 0.0610 |    5910 B |
