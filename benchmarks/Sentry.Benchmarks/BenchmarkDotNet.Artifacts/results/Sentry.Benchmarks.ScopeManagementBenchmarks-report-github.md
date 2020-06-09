``` ini

BenchmarkDotNet=v0.12.1, OS=centos 7
Intel Xeon CPU E3-1245 V2 3.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.200
  [Host]     : .NET Core 2.1.13 (CoreCLR 4.6.28008.01, CoreFX 4.6.28008.01), X64 RyuJIT
  Job-SFDEBM : .NET Core 2.1.13 (CoreCLR 4.6.28008.01, CoreFX 4.6.28008.01), X64 RyuJIT
  Job-IZSMTO : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT

InvocationCount=1  UnrollFactor=1  

```
|                        Method |        Job |       Runtime | Depth |       Mean |    Error |   StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------ |----------- |-------------- |------ |-----------:|---------:|---------:|------:|------:|------:|----------:|
| **&#39;Scope Push/Pop: Recursively&#39;** | **Job-SFDEBM** | **.NET Core 2.1** |     **1** |   **160.0 ns** |  **5.85 ns** | **10.55 ns** |     **-** |     **-** |     **-** |         **-** |
| &#39;Scope Push/Pop: Recursively&#39; | Job-IZSMTO | .NET Core 3.1 |     1 |   157.8 ns |  7.67 ns | 11.93 ns |     - |     - |     - |         - |
| **&#39;Scope Push/Pop: Recursively&#39;** | **Job-SFDEBM** | **.NET Core 2.1** |    **10** |   **200.5 ns** |  **6.54 ns** | **10.75 ns** |     **-** |     **-** |     **-** |         **-** |
| &#39;Scope Push/Pop: Recursively&#39; | Job-IZSMTO | .NET Core 3.1 |    10 |   262.5 ns |  9.21 ns | 14.61 ns |     - |     - |     - |         - |
| **&#39;Scope Push/Pop: Recursively&#39;** | **Job-SFDEBM** | **.NET Core 2.1** |   **100** | **1,268.6 ns** | **20.25 ns** | **18.94 ns** |     **-** |     **-** |     **-** |         **-** |
| &#39;Scope Push/Pop: Recursively&#39; | Job-IZSMTO | .NET Core 3.1 |   100 | 1,967.5 ns | 37.07 ns | 32.86 ns |     - |     - |     - |         - |
