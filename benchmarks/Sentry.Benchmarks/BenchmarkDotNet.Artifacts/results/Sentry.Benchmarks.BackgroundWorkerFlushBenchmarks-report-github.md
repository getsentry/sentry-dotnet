``` ini

BenchmarkDotNet=v0.11.1, OS=Windows 10.0.17134.765 (1803/April2018Update/Redstone4)
Intel Core i7-7920HQ CPU 3.10GHz (Kaby Lake), 1 CPU, 6 logical and 6 physical cores
.NET Core SDK=2.1.403
  [Host] : .NET Core 2.1.5 (CoreCLR 4.6.26919.02, CoreFX 4.6.26919.02), 64bit RyuJIT
  Core   : .NET Core 2.1.5 (CoreCLR 4.6.26919.02, CoreFX 4.6.26919.02), 64bit RyuJIT

Job=Core  Runtime=Core  InvocationCount=1  
UnrollFactor=1  

```
|                         Method | Items |     Mean |    Error |   StdDev | Allocated |
|------------------------------- |------ |---------:|---------:|---------:|----------:|
| **&#39;Enqueue event and FlushAsync&#39;** |     **1** | **128.3 us** | **7.478 us** | **21.58 us** |   **1.77 KB** |
| **&#39;Enqueue event and FlushAsync&#39;** |    **10** | **128.8 us** | **7.144 us** | **20.38 us** |   **1.77 KB** |
| **&#39;Enqueue event and FlushAsync&#39;** |   **100** | **167.8 us** | **9.121 us** | **26.17 us** |   **4.87 KB** |
