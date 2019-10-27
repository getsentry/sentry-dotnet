``` ini

BenchmarkDotNet=v0.11.1, OS=centos 7
Intel Xeon CPU E3-1245 V2 3.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.300
  [Host] : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT
  Core   : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT

Job=Core  Runtime=Core  InvocationCount=1  
UnrollFactor=1  

```
|                         Method | Items |     Mean |     Error |    StdDev | Allocated |
|------------------------------- |------ |---------:|----------:|----------:|----------:|
| **&#39;Enqueue event and FlushAsync&#39;** |     **1** | **1.914 us** | **0.1275 us** | **0.1193 us** |      **40 B** |
| **&#39;Enqueue event and FlushAsync&#39;** |    **10** | **1.844 us** | **0.0390 us** | **0.0619 us** |      **40 B** |
| **&#39;Enqueue event and FlushAsync&#39;** |   **100** | **1.709 us** | **0.0314 us** | **0.0278 us** |      **40 B** |
| **&#39;Enqueue event and FlushAsync&#39;** |  **1000** | **1.619 us** | **0.0315 us** | **0.0279 us** |      **40 B** |
