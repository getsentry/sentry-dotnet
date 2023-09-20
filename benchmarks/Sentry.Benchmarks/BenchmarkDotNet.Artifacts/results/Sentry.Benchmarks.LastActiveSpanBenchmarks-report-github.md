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
| **&#39;Create spans for scope access&#39;** |         **1** |  **32.34 μs** |    **26.58 μs** |  **1.457 μs** |  **4.7607** |  **1.5259** |  **16.68 KB** |
| **&#39;Create spans for scope access&#39;** |        **10** | **101.28 μs** |   **239.09 μs** | **13.105 μs** |  **9.1553** |  **2.5635** |  **39.21 KB** |
| **&#39;Create spans for scope access&#39;** |       **100** | **628.84 μs** | **1,267.56 μs** | **69.479 μs** | **59.5703** | **18.5547** | **266.14 KB** |
