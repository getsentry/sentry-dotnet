using Sentry.Testing;

namespace Sentry.Tests.Protocol.Context;

public class GpuTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public GpuTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        var sut = new Gpu
        {
            Name = "Sentry.Test.Gpu",
            Id = 123,
            VendorId = "321",
            VendorName = "Vendor name",
            MemorySize = 1_000,
            ApiType = "API Type",
            MultiThreadedRendering = true,
            Version = "Version 3232",
            NpotSupport = "Full NPOT",
            MaxTextureSize = 10000,
            GraphicsShaderLevel = "Shader Model 2.0",
            SupportsDrawCallInstancing = true,
            SupportsRayTracing = false,
            SupportsComputeShaders = true,
            SupportsGeometryShaders = true
        };

        var actual = sut.ToJsonString(_testOutputLogger);

        Assert.Equal(
            "{\"type\":\"gpu\"," +
            "\"name\":\"Sentry.Test.Gpu\"," +
            "\"id\":123," +
            "\"vendor_id\":\"321\"," +
            "\"vendor_name\":\"Vendor name\"," +
            "\"memory_size\":1000," +
            "\"api_type\":\"API Type\"," +
            "\"multi_threaded_rendering\":true," +
            "\"version\":\"Version 3232\"," +
            "\"npot_support\":\"Full NPOT\"," +
            "\"max_texture_size\":10000," +
            "\"graphics_shader_level\":\"Shader Model 2.0\"," +
            "\"supports_draw_call_instancing\":true," +
            "\"supports_ray_tracing\":false," +
            "\"supports_compute_shaders\":true," +
            "\"supports_geometry_shaders\":true}",
            actual);
    }

    [Fact]
    public void Clone_CopyValues()
    {
        var sut = new Gpu
        {
            Name = "Sentry.Test.Gpu",
            Id = 123,
            VendorId = "321",
            VendorName = "Vendor name",
            MemorySize = 1_000,
            ApiType = "API Type",
            MultiThreadedRendering = true,
            Version = "Version 3232",
            NpotSupport = "Full NPOT",
            MaxTextureSize = 10000,
            GraphicsShaderLevel = "Shader Model 2.0",
            SupportsDrawCallInstancing = true,
            SupportsRayTracing = false,
            SupportsComputeShaders = true,
            SupportsGeometryShaders = true
        };

        var clone = sut.Clone();

        Assert.Equal(sut.Name, clone.Name);
        Assert.Equal(sut.Id, clone.Id);
        Assert.Equal(sut.VendorId, clone.VendorId);
        Assert.Equal(sut.VendorName, clone.VendorName);
        Assert.Equal(sut.ApiType, clone.ApiType);
        Assert.Equal(sut.MultiThreadedRendering, clone.MultiThreadedRendering);
        Assert.Equal(sut.Version, clone.Version);
        Assert.Equal(sut.NpotSupport, clone.NpotSupport);
        Assert.Equal(sut.MaxTextureSize, clone.MaxTextureSize);
        Assert.Equal(sut.GraphicsShaderLevel, clone.GraphicsShaderLevel);
        Assert.Equal(sut.SupportsDrawCallInstancing, clone.SupportsDrawCallInstancing);
        Assert.Equal(sut.SupportsRayTracing, clone.SupportsRayTracing);
        Assert.Equal(sut.SupportsComputeShaders, clone.SupportsComputeShaders);
        Assert.Equal(sut.SupportsGeometryShaders, clone.SupportsGeometryShaders);
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializeObject_TestCase_SerializesAsExpected((Gpu gpu, string serialized) @case)
    {
        var actual = @case.gpu.ToJsonString(_testOutputLogger);

        Assert.Equal(@case.serialized, actual);
    }

    public static IEnumerable<object[]> TestCases()
    {
        yield return new object[] { (new Gpu(), "{\"type\":\"gpu\"}") };
        yield return new object[] { (new Gpu { Name = "some name" }, "{\"type\":\"gpu\",\"name\":\"some name\"}") };
        yield return new object[] { (new Gpu { Id = 1 }, "{\"type\":\"gpu\",\"id\":1}") };
        yield return new object[] { (new Gpu { VendorId = "1" }, "{\"type\":\"gpu\",\"vendor_id\":\"1\"}") };
        yield return new object[] { (new Gpu { VendorName = "some name" }, "{\"type\":\"gpu\",\"vendor_name\":\"some name\"}") };
        yield return new object[] { (new Gpu { MemorySize = 123 }, "{\"type\":\"gpu\",\"memory_size\":123}") };
        yield return new object[] { (new Gpu { ApiType = "some ApiType" }, "{\"type\":\"gpu\",\"api_type\":\"some ApiType\"}") };
        yield return new object[] { (new Gpu { MultiThreadedRendering = false }, "{\"type\":\"gpu\",\"multi_threaded_rendering\":false}") };
        yield return new object[] { (new Gpu { Version = "some version" }, "{\"type\":\"gpu\",\"version\":\"some version\"}") };
        yield return new object[] { (new Gpu { NpotSupport = "some npot_support" }, "{\"type\":\"gpu\",\"npot_support\":\"some npot_support\"}") };
        yield return new object[] { (new Gpu { MaxTextureSize = 10000 }, "{\"type\":\"gpu\",\"max_texture_size\":10000}") };
        yield return new object[] { (new Gpu { GraphicsShaderLevel = "Shader Model 2.0" }, "{\"type\":\"gpu\",\"graphics_shader_level\":\"Shader Model 2.0\"}") };
        yield return new object[] { (new Gpu { SupportsDrawCallInstancing = true }, "{\"type\":\"gpu\",\"supports_draw_call_instancing\":true}") };
        yield return new object[] { (new Gpu { SupportsRayTracing = false }, "{\"type\":\"gpu\",\"supports_ray_tracing\":false}") };
        yield return new object[] { (new Gpu { SupportsComputeShaders = true }, "{\"type\":\"gpu\",\"supports_compute_shaders\":true}") };
        yield return new object[] { (new Gpu { SupportsGeometryShaders = true }, "{\"type\":\"gpu\",\"supports_geometry_shaders\":true}") };
    }
}
