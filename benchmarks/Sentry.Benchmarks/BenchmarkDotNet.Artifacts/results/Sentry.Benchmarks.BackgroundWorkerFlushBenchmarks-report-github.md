``` ini

BenchmarkDotNet=v0.11.1, OS=Windows 10.0.17134.765 (1803/April2018Update/Redstone4)
Intel Core i7-7920HQ CPU 3.10GHz (Kaby Lake), 1 CPU, 6 logical and 6 physical cores
.NET Core SDK=2.1.403
  [Host] : .NET Core 2.1.5 (CoreCLR 4.6.26919.02, CoreFX 4.6.26919.02), 64bit RyuJIT
  Core   : .NET Core 2.1.5 (CoreCLR 4.6.26919.02, CoreFX 4.6.26919.02), 64bit RyuJIT

Job=Core  Runtime=Core  InvocationCount=1  
UnrollFactor=1  

```
|                         Method | Items |     Mean |     Error |    StdDev |   Median | Allocated |
|------------------------------- |------ |---------:|----------:|----------:|---------:|----------:|
| **&#39;Enqueue event and FlushAsync&#39;** |     **1** | **1.849 us** | **0.1244 us** | **0.3189 us** | **1.745 us** |      **40 B** |
| **&#39;Enqueue event and FlushAsync&#39;** |    **10** | **1.923 us** | **0.1066 us** | **0.2789 us** | **1.909 us** |      **40 B** |
| **&#39;Enqueue event and FlushAsync&#39;** |   **100** | **1.880 us** | **0.1151 us** | **0.3152 us** | **1.855 us** |      **40 B** |
| **&#39;Enqueue event and FlushAsync&#39;** |  **1000** | **1.885 us** | **0.1161 us** | **0.3118 us** | **1.770 us** |      **40 B** |
