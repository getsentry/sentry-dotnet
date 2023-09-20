``` ini

BenchmarkDotNet=v0.13.5, OS=macOS Ventura 13.5.2 (22G91) [Darwin 22.6.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK=7.0.306
  [Host]   : .NET 6.0.19 (6.0.1923.31806), Arm64 RyuJIT AdvSIMD
  ShortRun : .NET 6.0.19 (6.0.1923.31806), Arm64 RyuJIT AdvSIMD

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
|                          Method | SpanCount |      Mean |       Error |    StdDev |    Gen0 |    Gen1 | Allocated |
|-------------------------------- |---------- |----------:|------------:|----------:|--------:|--------:|----------:|
| **&#39;Create spans for scope access&#39;** |         **1** |  **31.42 μs** |    **28.15 μs** |  **1.543 μs** |  **4.6387** |  **1.5259** |  **16.64 KB** |
| **&#39;Create spans for scope access&#39;** |        **10** |  **95.37 μs** |   **169.78 μs** |  **9.306 μs** |  **9.1553** |  **2.5635** |  **39.17 KB** |
| **&#39;Create spans for scope access&#39;** |       **100** | **723.65 μs** | **1,013.19 μs** | **55.536 μs** | **59.5703** | **18.5547** | **266.09 KB** |
