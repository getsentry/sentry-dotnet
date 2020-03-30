namespace Sentry.Testing
{
    // Extensions as borrowed from:
    // https://github.com/serilog/serilog-settings-configuration/blob/dev/test/Serilog.Settings.Configuration.Tests/Support/Extensions.cs
    // Serilog License: https://github.com/serilog/serilog-settings-configuration/blob/dev/LICENSE
    public static class JsonStringExtensions
    {
        // netcore3.0 error:
        // Could not parse the JSON file. System.Text.Json.JsonReaderException : ''' is an invalid start of a property name. Expected a '"'
        public static string ToValidJson(this string str)
        {
#if NETCOREAPP3_1
            str = str.Replace('\'', '"');
#endif
            return str;
        }
    }
}
