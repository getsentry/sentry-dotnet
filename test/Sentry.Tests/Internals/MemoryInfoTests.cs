#if NETCOREAPP3_1_OR_GREATER
using System.Text.Json;

public class MemoryInfoTests
{
    [Fact]
    public void WriteTo()
    {
#if NET5_0_OR_GREATER
        var info = new MemoryInfo(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, true, false, new[] { TimeSpan.FromSeconds(1) });
#else
        var info = new MemoryInfo(1, 2, 3, 4, 5, 6);
#endif

        var stream = new MemoryStream();
        var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        info.WriteTo(writer, null);
        writer.Flush();
        var json = Encoding.UTF8.GetString(stream.ToArray());
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        //Validate json
        var serializer = new Newtonsoft.Json.JsonSerializer();
        serializer.Deserialize(new Newtonsoft.Json.JsonTextReader(new StringReader(json)));
    }
}
#endif
