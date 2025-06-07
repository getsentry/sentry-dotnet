#if NET8_0_OR_GREATER
using Graphs;
using Microsoft.Diagnostics.Utilities;

namespace Sentry.Tests;

public class MemDumpTests
{
    // private readonly ITestOutputHelper _testOutputHelper;
    // private readonly IDiagnosticLogger _testOutputLogger;

    // public MemDumpTests(ITestOutputHelper output, ITestOutputHelper testOutputHelper)
    // {
    //     _testOutputHelper = testOutputHelper;
    //     _testOutputLogger = new TestOutputDiagnosticLogger(output);
    // }

    [Fact]
    public void MemDump_Tests()
    {
        var path = "/Users/bruno/git/ConsoleApp1/ConsoleApp1/ConsoleApp1/20250526_133030_1.gcdump";
        var dump = new GCHeapDump(path);
        var graph = dump.MemoryGraph;
        Console.WriteLine(dump.CreationTool);
        Console.WriteLine(dump.CollectionLog);
        var ret = new Graph.SizeAndCount[(int)graph.NodeTypeIndexLimit];
        for (int i = 0; i < ret.Length; i++)
        {
            ret[i] = new Graph.SizeAndCount((NodeTypeIndex)i);
        }

        var nodeStorage = graph.AllocNodeStorage();
        for (NodeIndex idx = 0; idx < graph.NodeIndexLimit; idx++)
        {
            var node = graph.GetNode(idx, nodeStorage);
            var sizeAndCount = ret[(int)node.TypeIndex];
            sizeAndCount.Count++;
            sizeAndCount.Size += node.Size;
        }

        Array.Sort(ret, (Graph.SizeAndCount x, Graph.SizeAndCount y) => y.Size.CompareTo(x.Size));
        Console.WriteLine("<HistogramByType Size=\"{0}\" Count=\"{1}\">", dump.MemoryGraph.TotalSize, (int)dump.MemoryGraph.NodeIndexLimit);
        var typeStorage = new NodeType(graph);
        var sizeAndCounts = graph.GetHistogramByType();
        long minSize = 0;
        foreach (var sizeAndCount in sizeAndCounts)
        {
            if (sizeAndCount.Size <= minSize)
            {
                break;
            }

            var line = string.Format("Name=\"{0}\" Size=\"{1}\" Count=\"{2}\"",
                graph.GetType(sizeAndCount.TypeIdx, typeStorage).Name, sizeAndCount.Size, sizeAndCount.Count);
            Console.WriteLine(line);
        }
    }
}
#endif
