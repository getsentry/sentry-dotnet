``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i7-3770 CPU 3.40GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.1.300
  [Host] : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT
  Core   : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                        Method | Depth |         Mean |     Error |    StdDev | Allocated |
|------------------------------ |------ |-------------:|----------:|----------:|----------:|
| **&#39;Scope Push/Pop: Recursively&#39;** |     **1** |     **9.715 ns** | **0.1320 ns** | **0.1235 ns** |       **0 B** |
| **&#39;Scope Push/Pop: Recursively&#39;** |    **10** |    **67.780 ns** | **0.4568 ns** | **0.4049 ns** |       **0 B** |
| **&#39;Scope Push/Pop: Recursively&#39;** |   **100** | **1,186.196 ns** | **8.3338 ns** | **7.3877 ns** |       **0 B** |
