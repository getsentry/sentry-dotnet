using Sentry;
using System.Diagnostics;

// This gives us a chance to attach a debugger to the process when running the published app from the Debug directory.
// When doing that, you probably want to build and run the app in Debug mode from the command line.
//
// For example, on macOS:
//   dotnet publish -c Debug
//   ./bin/Debug/net7.0/osx-arm64/publish/Sentry.Samples.SingleFileApp
//
// On Windows:
//   dotnet publish -c Debug
//   .\bin\Debug\net7.0\win-x64\publish\Sentry.Samples.SingleFileApp.exe
Console.WriteLine("Current process ID: " + Process.GetCurrentProcess().Id);
Console.WriteLine("You can take our lives, but you can never take...");
Console.ReadKey();

SentrySdk.Init(options =>
{
    options.Dsn = "https://3bd169314f0947e187a372ad19682da6@o1197552.ingest.sentry.io/4505027036119040";
    options.Debug = true;
    options.SetBeforeSend((e, _) => {
        Console.WriteLine();
        Console.WriteLine(e.Message);
        if (e.DebugImages is { } images)
        {
            Console.WriteLine($"Debug Images: { images.Count }");
            foreach (var image in images)
            {
                Console.WriteLine($"  { image.Type } { image.DebugId } { image.DebugFile }");
            }
        }
        return e;
    });
});

try
{
    Console.WriteLine("OUR SYMBOLICATION!");
    throw new Exception("Sample exception.");
}
catch (Exception ex)
{
    SentrySdk.CaptureException(ex);
}
finally
{
    SentrySdk.FlushAsync(TimeSpan.FromSeconds(2)).Wait();
}
