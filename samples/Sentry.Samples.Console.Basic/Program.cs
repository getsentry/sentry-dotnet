using System;
using System.Threading.Tasks;
using static System.Console;

namespace Sentry.Samples.Console.Basic
{
    static class Program
    {
        static void Main()
        {
            var sentry = new HttpSentryClient();
            // This exception is captured and sent to Sentry
            throw null;
        }
    }
}
