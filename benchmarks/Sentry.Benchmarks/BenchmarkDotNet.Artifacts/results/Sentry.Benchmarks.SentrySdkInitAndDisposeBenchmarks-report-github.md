``` ini

BenchmarkDotNet=v0.11.0, OS=Windows 10.0.17134.228 (1803/April2018Update/Redstone4)
Intel Core i7-7920HQ CPU 3.10GHz (Max: 3.00GHz) (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.1.301
  [Host] : .NET Core 2.1.3 (CoreCLR 4.6.26725.06, CoreFX 4.6.26725.05), 64bit RyuJIT
  Core   : .NET Core 2.1.3 (CoreCLR 4.6.26725.06, CoreFX 4.6.26725.05), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                                  Method |       Mean |     Error |     StdDev |     Median | Scaled | ScaledSD |  Gen 0 |  Gen 1 | Allocated |
|-------------------------------------------------------- |-----------:|----------:|-----------:|-----------:|-------:|---------:|-------:|-------:|----------:|
|           &#39;Init/Dispose: no DSN provided, disabled SDK&#39; |   3.992 us | 0.0095 us |  0.0089 us |   3.990 us |   1.00 |     0.00 | 0.0992 |      - |     440 B |
| &#39;Init/Dispose: DSN provided via parameter, enabled SDK&#39; | 184.657 us | 0.2560 us |  0.2394 us | 184.702 us |  46.26 |     0.11 | 2.1973 | 0.4883 |    8649 B |
|            &#39;Init/Dispose: DSN via env var, enabled SDK&#39; | 179.130 us | 5.7005 us | 16.7185 us | 184.587 us |  44.87 |     4.17 | 2.1973 | 0.4883 |    8646 B |
