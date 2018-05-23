namespace Microsoft.Extensions.Logging
{
    internal class SentryLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new SentryLogger(categoryName);
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}
