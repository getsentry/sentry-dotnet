``` ini

BenchmarkDotNet=v0.11.0, OS=Windows 10.0.17134.165 (1803/April2018Update/Redstone4)
Intel Core i7-7920HQ CPU 3.10GHz (Kaby Lake), 1 CPU, 6 logical and 6 physical cores
.NET Core SDK=2.1.300
  [Host] : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT
  Core   : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                                  Method |       Mean |     Error |    StdDev | Scaled | ScaledSD |  Gen 0 |  Gen 1 | Allocated |
|-------------------------------------------------------- |-----------:|----------:|----------:|-------:|---------:|-------:|-------:|----------:|
|           &#39;Init/Dispose: no DSN provided, disabled SDK&#39; |   3.868 us | 0.0752 us | 0.0978 us |   1.00 |     0.00 | 0.0992 |      - |     440 B |
| &#39;Init/Dispose: DSN provided via parameter, enabled SDK&#39; | 107.566 us | 2.0511 us | 2.1946 us |  27.83 |     0.86 | 1.8311 | 0.1221 |    7401 B |
|            &#39;Init/Dispose: DSN via env var, enabled SDK&#39; | 107.330 us | 2.1364 us | 2.7019 us |  27.77 |     0.95 | 1.8311 | 0.1221 |    7402 B |
