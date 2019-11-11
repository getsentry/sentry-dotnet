``` ini

BenchmarkDotNet=v0.11.1, OS=centos 7
Intel Xeon CPU E3-1245 V2 3.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.300
  [Host] : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT
  Core   : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT

Job=Core  Runtime=Core  

```
|                                         Method | EventCount |        Mean |      Error |       StdDev |      Median |     Gen 0 |    Gen 1 |   Gen 2 |  Allocated |
|----------------------------------------------- |----------- |------------:|-----------:|-------------:|------------:|----------:|---------:|--------:|-----------:|
|    **&#39;CaptureException with duplicate detection&#39;** |          **1** |    **192.5 us** |   **9.423 us** |    **27.783 us** |    **184.9 us** |   **14.8926** |   **5.8594** |  **0.4883** |   **56.72 KB** |
| &#39;CaptureException without duplicate detection&#39; |          1 |    159.4 us |   3.173 us |     7.353 us |    157.3 us |   14.8926 |   5.6152 |  0.4883 |   56.67 KB |
|    **&#39;CaptureException with duplicate detection&#39;** |         **10** |  **1,787.9 us** |  **77.724 us** |   **226.724 us** |  **1,724.0 us** |  **152.3438** |  **62.5000** |  **3.9063** |  **567.21 KB** |
| &#39;CaptureException without duplicate detection&#39; |         10 |  1,766.5 us |  62.656 us |   181.776 us |  1,699.3 us |  154.2969 |  56.6406 |  3.9063 |  566.75 KB |
|    **&#39;CaptureException with duplicate detection&#39;** |        **100** | **18,842.3 us** | **946.665 us** | **2,791.263 us** | **17,592.1 us** | **1468.7500** | **531.2500** | **62.5000** | **5672.63 KB** |
| &#39;CaptureException without duplicate detection&#39; |        100 | 16,216.0 us | 318.604 us |   446.638 us | 16,118.1 us | 1500.0000 | 531.2500 | 31.2500 | 5667.54 KB |
