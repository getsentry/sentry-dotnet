``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i7-7920HQ CPU 3.10GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
Frequency=3023438 Hz, Resolution=330.7493 ns, Timer=TSC
.NET Core SDK=2.1.103
  [Host] : .NET Core 2.0.7 (CoreCLR 4.6.26328.01, CoreFX 4.6.26403.03), 64bit RyuJIT
  Core   : .NET Core 2.0.7 (CoreCLR 4.6.26328.01, CoreFX 4.6.26403.03), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                        Method | Depth |        Mean |      Error |     StdDev |  Gen 0 | Allocated |
|------------------------------ |------ |------------:|-----------:|-----------:|-------:|----------:|
| **&#39;Scope Push/Pop: Recursively&#39;** |     **1** |    **189.2 ns** |   **1.907 ns** |   **1.691 ns** | **0.0818** |     **344 B** |
| **&#39;Scope Push/Pop: Recursively&#39;** |    **10** |  **1,861.7 ns** |  **16.448 ns** |  **12.842 ns** | **0.8183** |    **3440 B** |
| **&#39;Scope Push/Pop: Recursively&#39;** |   **100** | **18,629.2 ns** | **116.727 ns** | **109.186 ns** | **8.1787** |   **34400 B** |
