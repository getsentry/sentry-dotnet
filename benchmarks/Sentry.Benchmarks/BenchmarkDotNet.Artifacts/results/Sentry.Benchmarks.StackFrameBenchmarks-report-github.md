``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1265/22H2/2022Update/SunValley2)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK=7.0.200
  [Host]     : .NET 6.0.14 (6.0.1423.7309), X64 RyuJIT AVX2
  Job-OZOGBN : .NET 6.0.14 (6.0.1423.7309), X64 RyuJIT AVX2

Runtime=.NET 6.0  InvocationCount=1  UnrollFactor=1  

```
|            Method |    N |     Mean |    Error |   StdDev | Allocated |
|------------------ |----- |---------:|---------:|---------:|----------:|
| ConfigureAppFrame | 1000 | 97.30 μs | 1.929 μs | 2.575 μs | 133.44 KB |
