<p align="center">
  <a href="https://sentry.io" target="_blank">
    <img src="https://raw.githubusercontent.com/getsentry/sentry-dotnet/main/.assets/sentry-logo.png" alt="Sentry logo" width="280">
  </a>
</p>

_Bad software is everywhere, and we're tired of it. Sentry is on a mission to help developers write better software faster, so we can get back to enjoying technology. If you want to join us [<kbd>**Check out our open positions**</kbd>](https://sentry.io/careers/)_

Sentry SDK for .NET
===========

[![build](https://github.com/getsentry/sentry-dotnet/workflows/build/badge.svg?branch=main)](https://github.com/getsentry/sentry-dotnet/actions?query=branch%3Amain)
[![codecov](https://codecov.io/gh/getsentry/sentry-dotnet/branch/main/graph/badge.svg)](https://codecov.io/gh/getsentry/sentry-dotnet)
[![Discord Chat](https://img.shields.io/discord/621778831602221064?logo=discord&logoColor=ffffff&color=7389D8)](https://discord.gg/PXa5Apfe7K)


|      Integrations             |    Downloads     |    NuGet Stable     |    NuGet Preview     |  Documentation |
| ----------------------------- | :-------------------: | :-------------------: | :-------------------: | :-------------------: |
|         **Sentry**            | [![Downloads](https://img.shields.io/nuget/dt/Sentry.svg)](https://www.nuget.org/packages/Sentry) | [![NuGet](https://img.shields.io/nuget/v/Sentry.svg)](https://www.nuget.org/packages/Sentry)   |    [![NuGet](https://img.shields.io/nuget/vpre/Sentry.svg)](https://www.nuget.org/packages/Sentry)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/) |
| **Sentry.Extensions.Logging** | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/extensions-logging/) |
|     **Sentry.AspNetCore**     | [![Downloads](https://img.shields.io/nuget/dt/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore) | [![NuGet](https://img.shields.io/nuget/v/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/aspnetcore/) |
|     **Sentry.AspNetCore.Grpc**     | [![Downloads](https://img.shields.io/nuget/dt/Sentry.AspNetCore.Grpc.svg)](https://www.nuget.org/packages/Sentry.AspNetCore.Grpc) | [![NuGet](https://img.shields.io/nuget/v/Sentry.AspNetCore.Grpc.svg)](https://www.nuget.org/packages/Sentry.AspNetCore.Grpc)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.Grpc.svg)](https://www.nuget.org/packages/Sentry.AspNetCore.Grpc)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/aspnetcore/) |
|     **Sentry.Google.Cloud.Functions**     | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Google.Cloud.Functions.svg)](https://www.nuget.org/packages/Sentry.Google.Cloud.Functions) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Google.Cloud.Functions.svg)](https://www.nuget.org/packages/Sentry.Google.Cloud.Functions)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Google.Cloud.Functions.svg)](https://www.nuget.org/packages/Sentry.Google.Cloud.Functions)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/gcp-functions/) |
|     **Sentry.AspNet**     | [![Downloads](https://img.shields.io/nuget/dt/Sentry.AspNet.svg)](https://www.nuget.org/packages/Sentry.AspNet) | [![NuGet](https://img.shields.io/nuget/v/Sentry.AspNet.svg)](https://www.nuget.org/packages/Sentry.AspNet)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNet.svg)](https://www.nuget.org/packages/Sentry.AspNet)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/aspnet) |
|     **Sentry.EntityFramework**     | [![Downloads](https://img.shields.io/nuget/dt/Sentry.EntityFramework.svg)](https://www.nuget.org/packages/Sentry.EntityFramework) | [![NuGet](https://img.shields.io/nuget/v/Sentry.EntityFramework.svg)](https://www.nuget.org/packages/Sentry.EntityFramework)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.EntityFramework.svg)](https://www.nuget.org/packages/Sentry.EntityFramework)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/entityframework) |
| **Sentry.Serilog**            | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Serilog.svg)](https://www.nuget.org/packages/Serilog) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Serilog.svg)](https://www.nuget.org/packages/Sentry.Serilog)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Serilog.svg)](https://www.nuget.org/packages/Sentry.Serilog)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/serilog) |
| **Sentry.Log4Net**            | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/log4net) |
| **Sentry.NLog**               | [![Downloads](https://img.shields.io/nuget/dt/Sentry.NLog.svg)](https://www.nuget.org/packages/Sentry.NLog) | [![NuGet](https://img.shields.io/nuget/v/Sentry.NLog.svg)](https://www.nuget.org/packages/Sentry.NLog)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.NLog.svg)](https://www.nuget.org/packages/Sentry.NLog)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/guides/nlog) |
| **Sentry.DiagnosticSource**               | [![Downloads](https://img.shields.io/nuget/dt/Sentry.DiagnosticSource.svg)](https://www.nuget.org/packages/Sentry.DiagnosticSource) | [![NuGet](https://img.shields.io/nuget/v/Sentry.DiagnosticSource.svg)](https://www.nuget.org/packages/Sentry.DiagnosticSource)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.DiagnosticSource.svg)](https://www.nuget.org/packages/Sentry.DiagnosticSource)   | [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/performance/instrumentation/automatic-instrumentation/#diagnosticsource-integration) |

## More Sentry .NET Integrations

Sentry offers other integrations that are not part of this repository:

* [Sentry.Minidump](https://github.com/getsentry/sentry-dotnet-minidump): Capture Minidumps on Windows, macOS and Linux.
* [Sentry.Xamarin](https://github.com/getsentry/sentry-xamarin): `Xamarin` native and `Xamarin.Forms` integration to get device data, breadcrumbs, and more.

Looking for something else? Let us know by [raising an issue](https://github.com/getsentry/sentry-dotnet/issues/new).

## Documentation

Each NuGet package in the table above has its custom view of the docs. Click on the badge to find the best documentation for your use case.

Sentry has extensive documentation for its SDKs on [https://docs.sentry.io](https://docs.sentry.io/platforms/dotnet/).
[The .NET API (DocFX) is generated on each merge to main and pushed to GitHub pages](https://getsentry.github.io/sentry-dotnet/index.html) Documentation.

### Samples

**Consider taking a look at the _[samples](https://github.com/getsentry/sentry-dotnet/tree/main/samples)_ directory for different types of apps and example usages of the SDK.**

Looking for more samples? Check out [this repository](https://github.com/getsentry/examples).

## Compatibility

The packages target **.NET Standard 2.0** and **.NET Framework 4.6.1**. That means [it is compatible with](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) the following versions or newer:

* .NET Framework 4.6.1
* .NET Core 2.0
* Mono 5.4
* Xamarin.Android 8.0
* Xamarin.Mac 3.8
* Xamarin.iOS 10.14
* Universal Windows Platform 10.0.16299

Of those, we've tested (we run our unit/integration tests) against:

* .NET Framework 4.8 on Windows
* Mono 6.6 on macOS and Linux
* .NET Core 2.1 on Windows, macOS and Linux
* .NET Core 3.1 on Windows, macOS and Linux
* .NET 5 on Windows, macOS and Linux

### Sentry Protocol

For more details, please: **refer to the [documentation](https://getsentry.github.io/sentry-dotnet/index.html)**

### Legacy frameworks

Sentry's [Raven SDK](https://github.com/getsentry/raven-csharp/), battle tested with over 3.500.000 downloads on NuGet has support to .NET Framework 3.5+ and is compatible with all versions of Sentry including [sentry.io](sentry.io).

## Resources

* [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/)
* [![Forum](https://img.shields.io/badge/forum-sentry-green.svg)](https://forum.sentry.io/c/sdks)
* [![Discord Chat](https://img.shields.io/discord/621778831602221064?logo=discord&logoColor=ffffff&color=7389D8)](https://discord.gg/PXa5Apfe7K)
* [![Stack Overflow](https://img.shields.io/badge/stack%20overflow-sentry-green.svg)](http://stackoverflow.com/questions/tagged/sentry)
* [![Twitter Follow](https://img.shields.io/twitter/follow/getsentry?label=getsentry&style=social)](https://twitter.com/intent/follow?screen_name=getsentry)
