using Sentry;
using System.Diagnostics;

// This gives us a chance to attach a debugger to the process when running the published app from the Debug directory.
// When doing that, you probably want to build and run the app in Debug mode from the command line:
//   dotnet publish -c Debug
//   ./bin/Debug/net7.0/osx-arm64/publish/Sentry.Samples.SingleFileApp
Console.WriteLine("Current process ID: " + Process.GetCurrentProcess().Id);
Console.WriteLine("You can take our lives, but you can never take...");
Console.ReadKey();

try
{
    Console.WriteLine("OUR SYMBOLICATION!");
    throw new Exception("Line numbers please.");
}
catch (Exception ex)
{
    SentrySdk.CaptureException(ex);
}
finally
{
    SentrySdk.FlushAsync(TimeSpan.FromSeconds(2)).Wait();
}
