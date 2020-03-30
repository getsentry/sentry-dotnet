using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Sentry.Testing
{
    // JsonStringConfigSource as borrowed from Serilog:
    // https://github.com/serilog/serilog-settings-configuration/blob/dev/test/Serilog.Settings.Configuration.Tests/Support/Extensions.cs
    // Serilog License: https://github.com/serilog/serilog-settings-configuration/blob/dev/LICENSE
    public class JsonStringConfigSource : IConfigurationSource
    {
        readonly string _json;

        public JsonStringConfigSource(string json)
        {
            _json = json;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new JsonStringConfigProvider(_json);
        }

        public static IConfigurationSection LoadSection(string json, string section)
        {
            return new ConfigurationBuilder().Add(new JsonStringConfigSource(json)).Build().GetSection(section);
        }

        public static IDictionary<string, string> LoadData(string json)
        {
            var provider = new JsonStringConfigProvider(json);
            provider.Load();
            return provider.Data;
        }

        private class JsonStringConfigProvider : JsonConfigurationProvider
        {
            readonly string _json;

            public JsonStringConfigProvider(string json) : base(new JsonConfigurationSource { Optional = true })
            {
                _json = json;
            }

            public new IDictionary<string, string> Data => base.Data;

            public override void Load()
            {
                Load(StringToStream(_json.ToValidJson()));
            }

            static Stream StringToStream(string str)
            {
                var memStream = new MemoryStream();
                var textWriter = new StreamWriter(memStream);
                textWriter.Write(str);
                textWriter.Flush();
                memStream.Seek(0, SeekOrigin.Begin);

                return memStream;
            }
        }
    }
}
