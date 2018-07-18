``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i7-3770 CPU 3.40GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.1.300
  [Host] : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT
  Core   : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                                  Method |       Mean |     Error |    StdDev | Scaled | ScaledSD |  Gen 0 |  Gen 1 | Allocated |
|-------------------------------------------------------- |-----------:|----------:|----------:|-------:|---------:|-------:|-------:|----------:|
|           &#39;Init/Dispose: no DSN provided, disabled SDK&#39; |   5.574 us | 0.0874 us | 0.0774 us |   1.00 |     0.00 | 0.0992 |      - |     440 B |
| &#39;Init/Dispose: DSN provided via parameter, enabled SDK&#39; | 154.901 us | 2.9060 us | 2.7183 us |  27.80 |     0.60 | 1.7090 | 0.2441 |    7193 B |
|            &#39;Init/Dispose: DSN via env var, enabled SDK&#39; | 153.578 us | 2.7869 us | 2.6069 us |  27.56 |     0.58 | 1.7090 | 0.2441 |    7183 B |
