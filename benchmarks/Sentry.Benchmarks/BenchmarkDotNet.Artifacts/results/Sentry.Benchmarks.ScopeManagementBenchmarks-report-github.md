``` ini

BenchmarkDotNet=v0.11.1, OS=centos 7
Intel Xeon CPU E3-1245 V2 3.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.300
  [Host] : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT
  Core   : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT

Job=Core  Runtime=Core  InvocationCount=1  
UnrollFactor=1  

```
|                        Method | Depth |     Mean |    Error |    StdDev | Allocated |
|------------------------------ |------ |---------:|---------:|----------:|----------:|
| **&#39;Scope Push/Pop: Recursively&#39;** |     **1** | **127.4 ns** | **5.487 ns** |  **8.704 ns** |       **0 B** |
| **&#39;Scope Push/Pop: Recursively&#39;** |    **10** | **202.2 ns** | **6.609 ns** | **14.368 ns** |       **0 B** |
| **&#39;Scope Push/Pop: Recursively&#39;** |   **100** | **777.1 ns** | **4.404 ns** |  **3.678 ns** |       **0 B** |
