[![Sentry](https://raw.githubusercontent.com/getsentry/sentry-dotnet/main/.assets/sentry-wordmark-dark-280x84.png)](https://sentry.io/?utm_source=github&utm_medium=logo)

_Bad software is everywhere, and we're tired of it. Sentry is on a mission to help developers write better software faster, so we can get back to enjoying technology. If you want to join us, [**check out our open positions**](https://sentry.io/careers/)._

Sentry SDK for .NET
===========

[![build](https://github.com/getsentry/sentry-dotnet/workflows/build/badge.svg?branch=main)](https://github.com/getsentry/sentry-dotnet/actions?query=branch%3Amain)
[![Discord Chat](https://img.shields.io/discord/621778831602221064?logo=discord&logoColor=ffffff&color=7389D8)](https://discord.gg/PXa5Apfe7K)


|            Integrations            |       Downloads       |      NuGet Stable     |     NuGet Preview     |     Documentation     |
| ---------------------------------- | :-------------------: | :-------------------: | :-------------------: | :-------------------: |
| **Sentry**                         | [![Downloads](https://img.shields.io/nuget/dt/Sentry.svg)](https://www.nuget.org/packages/Sentry) | [![NuGet](https://img.shields.io/nuget/v/Sentry.svg)](https://www.nuget.org/packages/Sentry)   |    [![NuGet](https://img.shields.io/nuget/vpre/Sentry.svg)](https://www.nuget.org/packages/Sentry)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/) |
| **Sentry.AspNet**                  | [![Downloads](https://img.shields.io/nuget/dt/Sentry.AspNet.svg)](https://www.nuget.org/packages/Sentry.AspNet) | [![NuGet](https://img.shields.io/nuget/v/Sentry.AspNet.svg)](https://www.nuget.org/packages/Sentry.AspNet)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNet.svg)](https://www.nuget.org/packages/Sentry.AspNet)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/aspnet) |
| **Sentry.AspNetCore**              | [![Downloads](https://img.shields.io/nuget/dt/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore) | [![NuGet](https://img.shields.io/nuget/v/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/aspnetcore/) |
| **Sentry.AspNetCore.Grpc**         | [![Downloads](https://img.shields.io/nuget/dt/Sentry.AspNetCore.Grpc.svg)](https://www.nuget.org/packages/Sentry.AspNetCore.Grpc) | [![NuGet](https://img.shields.io/nuget/v/Sentry.AspNetCore.Grpc.svg)](https://www.nuget.org/packages/Sentry.AspNetCore.Grpc)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.Grpc.svg)](https://www.nuget.org/packages/Sentry.AspNetCore.Grpc)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/aspnetcore/) |
| **Sentry.AzureFunctions.Worker**   | [![Downloads](https://img.shields.io/nuget/dt/Sentry.AzureFunctions.Worker.svg)](https://www.nuget.org/packages/Sentry.AzureFunctions.Worker.Grpc) | [![NuGet](https://img.shields.io/nuget/v/Sentry.AzureFunctions.Worker.svg)](https://www.nuget.org/packages/Sentry.AzureFunctions.Worker)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AzureFunctions.Worker.svg)](https://www.nuget.org/packages/Sentry.AzureFunctions.Worker)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/azure-functions-worker/) |
| **Sentry.DiagnosticSource**        | [![Downloads](https://img.shields.io/nuget/dt/Sentry.DiagnosticSource.svg)](https://www.nuget.org/packages/Sentry.DiagnosticSource) | [![NuGet](https://img.shields.io/nuget/v/Sentry.DiagnosticSource.svg)](https://www.nuget.org/packages/Sentry.DiagnosticSource)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.DiagnosticSource.svg)](https://www.nuget.org/packages/Sentry.DiagnosticSource)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/performance/instrumentation/automatic-instrumentation/#diagnosticsource-integration) |
| **Sentry.EntityFramework**         | [![Downloads](https://img.shields.io/nuget/dt/Sentry.EntityFramework.svg)](https://www.nuget.org/packages/Sentry.EntityFramework) | [![NuGet](https://img.shields.io/nuget/v/Sentry.EntityFramework.svg)](https://www.nuget.org/packages/Sentry.EntityFramework)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.EntityFramework.svg)](https://www.nuget.org/packages/Sentry.EntityFramework)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/entityframework) |
| **Sentry.Extensions.Logging**      | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/extensions-logging/) |
| **Sentry.Google.Cloud.Functions**  | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Google.Cloud.Functions.svg)](https://www.nuget.org/packages/Sentry.Google.Cloud.Functions) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Google.Cloud.Functions.svg)](https://www.nuget.org/packages/Sentry.Google.Cloud.Functions)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Google.Cloud.Functions.svg)](https://www.nuget.org/packages/Sentry.Google.Cloud.Functions)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/gcp-functions/) |
| **Sentry.Log4Net**                 | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/log4net) |
| **Sentry.Maui**                    | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Maui.svg)](https://www.nuget.org/packages/Sentry.Maui) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Maui.svg)](https://www.nuget.org/packages/Sentry.Maui)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Maui.svg)](https://www.nuget.org/packages/Sentry.Maui)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/maui) |
| **Sentry.NLog**                    | [![Downloads](https://img.shields.io/nuget/dt/Sentry.NLog.svg)](https://www.nuget.org/packages/Sentry.NLog) | [![NuGet](https://img.shields.io/nuget/v/Sentry.NLog.svg)](https://www.nuget.org/packages/Sentry.NLog)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.NLog.svg)](https://www.nuget.org/packages/Sentry.NLog)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/nlog) |
| **Sentry.Serilog**                 | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Serilog.svg)](https://www.nuget.org/packages/Serilog) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Serilog.svg)](https://www.nuget.org/packages/Sentry.Serilog)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Serilog.svg)](https://www.nuget.org/packages/Sentry.Serilog)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/serilog) |

## More Sentry .NET Integrations

Sentry offers other integrations that are not part of this repository:

* [Sentry.Minidump](https://github.com/getsentry/sentry-dotnet-minidump): Capture Minidumps on Windows, macOS and Linux
* [Sentry.Unity](https://github.com/getsentry/sentry-unity): Unity integrations
* [Sentry.Xamarin](https://github.com/getsentry/sentry-xamarin): Xamarin native and Xamarin.Forms integrations

Looking for something else? Let us know by [raising an issue](https://github.com/getsentry/sentry-dotnet/issues/new).

## Documentation

Each NuGet package in the table above has its custom view of the docs. Click on the badge to find the best documentation for your use case.

Sentry has extensive documentation for its SDKs on [https://docs.sentry.io](https://docs.sentry.io/platforms/dotnet/).

Additionally, our [.NET API reference docs](https://getsentry.github.io/sentry-dotnet/index.html) are generated and deployed on each merge to main.

### Samples

**Consider taking a look at the _[samples](https://github.com/getsentry/sentry-dotnet/tree/main/samples)_ directory for different types of apps and example usages of the SDK.**

Looking for more samples? Check out [this repository](https://github.com/getsentry/examples).

### Talks

* On.NET [Error monitoring with Sentry for .NET MAUI](https://www.youtube.com/watch?v=8YmEC4iKD2I)
* .NET Conf [focus on MAUI](https://www.youtube.com/watch?v=RW3hiukVXZQ&list=PLdo4fOcmZ0oWePZU3W162NJ9vcXqgpMVc) 

## Compatibility

The packages target **.NET Standard 2.0** and **.NET Framework 4.6.1**.
They also include targets such as **.NET 5**, **.NET 6** and platform-specific targets where appropriate.
That means [they are compatible with](https://docs.microsoft.com/dotnet/standard/net-standard) the following versions _or newer_:

* .NET 5.0
* .NET Core 2.0
* .NET Framework 4.6.1
* Mono 5.4
* Xamarin.Android 8.0
* Xamarin.iOS 10.14
* Xamarin.Mac 3.8
* Universal Windows Platform 10.0.16299

Of those, we run our unit and integration tests against the following:

* .NET 7 on Windows, macOS and Linux
* .NET 6 on Windows, macOS and Linux
* .NET Core 3.1 on Windows, macOS and Linux
* .NET Framework 4.8 on Windows
* Mono 6.12 on macOS and Linux

### Sentry Protocol

For more details, please: **refer to the [documentation](https://getsentry.github.io/sentry-dotnet/index.html)**

### Legacy frameworks

Sentry's [Raven SDK](https://github.com/getsentry/raven-csharp/) is still available, and recommended for use with .NET Framework 3.5 to 4.6.0.

## Resources
* [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/)
* [![Discussions](https://img.shields.io/github/discussions/getsentry/sentry-dotnet.svg)](https://github.com/getsentry/sentry-dotnet/discussions)
* [![Discord Chat](https://img.shields.io/discord/621778831602221064?logo=discord&logoColor=ffffff&color=7389D8)](https://discord.gg/PXa5Apfe7K)
* [![Stack Overflow](https://img.shields.io/badge/stack%20overflow-sentry-green.svg)](http://stackoverflow.com/questions/tagged/sentry)
* [![Twitter Follow](https://img.shields.io/twitter/follow/getsentry?label=getsentry&style=social)](https://twitter.com/intent/follow?screen_name=getsentry)
