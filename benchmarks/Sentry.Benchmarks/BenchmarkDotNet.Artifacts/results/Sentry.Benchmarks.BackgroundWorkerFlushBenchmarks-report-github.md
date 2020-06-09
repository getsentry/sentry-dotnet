``` ini

BenchmarkDotNet=v0.12.1, OS=centos 7
Intel Xeon CPU E3-1245 V2 3.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.200
  [Host]     : .NET Core 2.1.13 (CoreCLR 4.6.28008.01, CoreFX 4.6.28008.01), X64 RyuJIT
  Job-SFDEBM : .NET Core 2.1.13 (CoreCLR 4.6.28008.01, CoreFX 4.6.28008.01), X64 RyuJIT
  Job-IZSMTO : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT

InvocationCount=1  UnrollFactor=1  

```
|                         Method |        Job |       Runtime | Items |       Mean |      Error |     StdDev |     Median | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------- |----------- |-------------- |------ |-----------:|-----------:|-----------:|-----------:|------:|------:|------:|----------:|
| **&#39;Enqueue event and FlushAsync&#39;** | **Job-SFDEBM** | **.NET Core 2.1** |     **1** |   **1.732 μs** |  **0.0353 μs** |  **0.0713 μs** |   **1.724 μs** |     **-** |     **-** |     **-** |      **40 B** |
| &#39;Enqueue event and FlushAsync&#39; | Job-IZSMTO | .NET Core 3.1 |     1 |   2.978 μs |  0.0605 μs |  0.0566 μs |   2.957 μs |     - |     - |     - |     120 B |
| **&#39;Enqueue event and FlushAsync&#39;** | **Job-SFDEBM** | **.NET Core 2.1** |    **10** |   **1.900 μs** |  **0.1007 μs** |  **0.2773 μs** |   **1.774 μs** |     **-** |     **-** |     **-** |      **40 B** |
| &#39;Enqueue event and FlushAsync&#39; | Job-IZSMTO | .NET Core 3.1 |    10 |   3.027 μs |  0.0637 μs |  0.0654 μs |   3.024 μs |     - |     - |     - |     120 B |
| **&#39;Enqueue event and FlushAsync&#39;** | **Job-SFDEBM** | **.NET Core 2.1** |   **100** |   **1.733 μs** |  **0.0446 μs** |  **0.1175 μs** |   **1.701 μs** |     **-** |     **-** |     **-** |      **40 B** |
| &#39;Enqueue event and FlushAsync&#39; | Job-IZSMTO | .NET Core 3.1 |   100 |   2.994 μs |  0.0644 μs |  0.0689 μs |   2.981 μs |     - |     - |     - |     120 B |
| **&#39;Enqueue event and FlushAsync&#39;** | **Job-SFDEBM** | **.NET Core 2.1** |  **1000** |  **80.236 μs** | **13.1879 μs** | **38.8849 μs** |  **89.102 μs** |     **-** |     **-** |     **-** |      **40 B** |
| &#39;Enqueue event and FlushAsync&#39; | Job-IZSMTO | .NET Core 3.1 |  1000 | 120.977 μs | 13.4044 μs | 39.5232 μs | 125.910 μs |     - |     - |     - |     120 B |
