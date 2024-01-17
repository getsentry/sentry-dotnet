<p align="center">
  <a href="https://sentry.io" target="_blank" align="left">
    <img src="https://raw.githubusercontent.com/getsentry/sentry-unity/main/.github/sentry-wordmark-dark-400x119.svg?utm_source=github&utm_medium=logo" width="280">
  </a>
  <br />
</p>
<p align="center">

_Bad software is everywhere, and we're tired of it. Sentry is on a mission to help developers write better software faster, so we can get back to enjoying technology. If you want to join us, [**check out our open positions**](https://sentry.io/careers/)._

Sentry SDK for .NET
===========

[![build](https://github.com/getsentry/sentry-dotnet/workflows/build/badge.svg?branch=main)](https://github.com/getsentry/sentry-dotnet/actions?query=branch%3Amain)
[![codecov](https://codecov.io/gh/getsentry/sentry-dotnet/branch/main/graph/badge.svg)](https://codecov.io/gh/getsentry/sentry-dotnet)
[![Discord Chat](https://img.shields.io/discord/621778831602221064?logo=discord&logoColor=ffffff&color=7389D8)](https://discord.gg/PXa5Apfe7K)


|            Integrations            |       Downloads       |      NuGet Stable     |     NuGet Preview     |     Documentation     |
| ---------------------------------- | :-------------------: | :-------------------: | :-------------------: | :-------------------: |
| **Sentry**                         | [![Downloads](https://img.shields.io/nuget/dt/Sentry.svg)](https://www.nuget.org/packages/Sentry) | [![NuGet](https://img.shields.io/nuget/v/Sentry.svg)](https://www.nuget.org/packages/Sentry)   |    [![NuGet](https://img.shields.io/nuget/vpre/Sentry.svg)](https://www.nuget.org/packages/Sentry)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/) |
| **Sentry.AspNetCore**              | [![Downloads](https://img.shields.io/nuget/dt/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore) | [![NuGet](https://img.shields.io/nuget/v/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/aspnetcore/) |
| **Sentry.AspNetCore.Grpc**         | [![Downloads](https://img.shields.io/nuget/dt/Sentry.AspNetCore.Grpc.svg)](https://www.nuget.org/packages/Sentry.AspNetCore.Grpc) | [![NuGet](https://img.shields.io/nuget/v/Sentry.AspNetCore.Grpc.svg)](https://www.nuget.org/packages/Sentry.AspNetCore.Grpc)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.Grpc.svg)](https://www.nuget.org/packages/Sentry.AspNetCore.Grpc)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/aspnetcore/) |
| **Sentry.AspNet**                  | [![Downloads](https://img.shields.io/nuget/dt/Sentry.AspNet.svg)](https://www.nuget.org/packages/Sentry.AspNet) | [![NuGet](https://img.shields.io/nuget/v/Sentry.AspNet.svg)](https://www.nuget.org/packages/Sentry.AspNet)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNet.svg)](https://www.nuget.org/packages/Sentry.AspNet)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/aspnet) |
| **Sentry.DiagnosticSource**        | [![Downloads](https://img.shields.io/nuget/dt/Sentry.DiagnosticSource.svg)](https://www.nuget.org/packages/Sentry.DiagnosticSource) | [![NuGet](https://img.shields.io/nuget/v/Sentry.DiagnosticSource.svg)](https://www.nuget.org/packages/Sentry.DiagnosticSource)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.DiagnosticSource.svg)](https://www.nuget.org/packages/Sentry.DiagnosticSource)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/performance/instrumentation/automatic-instrumentation/#diagnosticsource-integration) |
| **Sentry.EntityFramework**         | [![Downloads](https://img.shields.io/nuget/dt/Sentry.EntityFramework.svg)](https://www.nuget.org/packages/Sentry.EntityFramework) | [![NuGet](https://img.shields.io/nuget/v/Sentry.EntityFramework.svg)](https://www.nuget.org/packages/Sentry.EntityFramework)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.EntityFramework.svg)](https://www.nuget.org/packages/Sentry.EntityFramework)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/entityframework) |
 **Sentry.Azure.Functions.Worker**   | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Azure.Functions.Worker.svg)](https://www.nuget.org/packages/Sentry.Azure.Functions.Worker) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Azure.Functions.Worker.svg)](https://www.nuget.org/packages/Sentry.Azure.Functions.Worker)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Azure.Functions.Worker.svg)](https://www.nuget.org/packages/Sentry.Azure.Functions.Worker)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/azure-functions-worker/) |
| **Sentry.Google.Cloud.Functions**  | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Google.Cloud.Functions.svg)](https://www.nuget.org/packages/Sentry.Google.Cloud.Functions) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Google.Cloud.Functions.svg)](https://www.nuget.org/packages/Sentry.Google.Cloud.Functions)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Google.Cloud.Functions.svg)](https://www.nuget.org/packages/Sentry.Google.Cloud.Functions)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/google-cloud-functions/) |
| **Sentry.Maui**                    | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Maui.svg)](https://www.nuget.org/packages/Sentry.Maui) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Maui.svg)](https://www.nuget.org/packages/Sentry.Maui)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Maui.svg)](https://www.nuget.org/packages/Sentry.Maui)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/maui) |
| **Sentry.Serilog**                 | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Serilog.svg)](https://www.nuget.org/packages/Serilog) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Serilog.svg)](https://www.nuget.org/packages/Sentry.Serilog)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Serilog.svg)](https://www.nuget.org/packages/Sentry.Serilog)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/serilog) |
| **Sentry.NLog**                    | [![Downloads](https://img.shields.io/nuget/dt/Sentry.NLog.svg)](https://www.nuget.org/packages/Sentry.NLog) | [![NuGet](https://img.shields.io/nuget/v/Sentry.NLog.svg)](https://www.nuget.org/packages/Sentry.NLog)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.NLog.svg)](https://www.nuget.org/packages/Sentry.NLog)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/nlog) |
| **Sentry.Log4Net**                 | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/log4net) |
| **Sentry.OpenTelemetry**                 | [![Downloads](https://img.shields.io/nuget/dt/Sentry.OpenTelemetry.svg)](https://www.nuget.org/packages/Sentry.OpenTelemetry) | [![NuGet](https://img.shields.io/nuget/v/Sentry.OpenTelemetry.svg)](https://www.nuget.org/packages/Sentry.OpenTelemetry)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.OpenTelemetry.svg)](https://www.nuget.org/packages/Sentry.OpenTelemetry)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/performance/instrumentation/opentelemetry/) |

## More Sentry .NET Integrations

Sentry offers other integrations that are not part of this repository:

* [Sentry.Unity](https://github.com/getsentry/sentry-unity): Unity integrations
* [Sentry.Xamarin](https://github.com/getsentry/sentry-xamarin): Xamarin native and Xamarin.Forms integrations

Looking for something else? Let us know by [raising an issue](https://github.com/getsentry/sentry-dotnet/issues/new).

## Documentation

Each NuGet package in the table above has its custom view of the docs. Click on the badge to find the best documentation for your use case.

Sentry has extensive documentation for its SDKs on [https://docs.sentry.io](https://docs.sentry.io/platforms/dotnet/).

### Samples

**Consider taking a look at the _[samples](https://github.com/getsentry/sentry-dotnet/tree/main/samples)_ directory for different types of apps and example usages of the SDK.**

### Talks

* On.NET [Error monitoring with Sentry for .NET MAUI](https://www.youtube.com/watch?v=8YmEC4iKD2I)
* .NET Conf [focus on MAUI](https://www.youtube.com/watch?v=RW3hiukVXZQ&list=PLdo4fOcmZ0oWePZU3W162NJ9vcXqgpMVc)

## Resources
* [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/)
* [![Discussions](https://img.shields.io/github/discussions/getsentry/sentry-dotnet.svg)](https://github.com/getsentry/sentry-dotnet/discussions)
* [![Discord Chat](https://img.shields.io/discord/621778831602221064?logo=discord&logoColor=ffffff&color=7389D8)](https://discord.gg/PXa5Apfe7K)
* [![Stack Overflow](https://img.shields.io/badge/stack%20overflow-sentry-green.svg)](http://stackoverflow.com/questions/tagged/sentry)
* [![Twitter Follow](https://img.shields.io/twitter/follow/getsentry?label=getsentry&style=social)](https://twitter.com/intent/follow?screen_name=getsentry)
