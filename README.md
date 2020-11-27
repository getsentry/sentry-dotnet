<p align="center">
  <a href="https://sentry.io" target="_blank" align="center">
    <img src="https://sentry-brand.storage.googleapis.com/sentry-logo-black.png" width="280">
  </a>
  <br />
</p>

Sentry SDK for .NET 
===========

[![build](https://github.com/getsentry/sentry-dotnet/workflows/build/badge.svg?branch=main)](https://github.com/getsentry/sentry-dotnet/actions?query=branch%3Amain)
[![Travis](https://travis-ci.org/getsentry/sentry-dotnet.svg?branch=main)](https://travis-ci.org/getsentry/sentry-dotnet)
[![AppVeyor](https://ci.appveyor.com/api/projects/status/wu055n0n4u8p20p2/branch/main?svg=true)](https://ci.appveyor.com/project/sentry/sentry-dotnet/branch/main)
[![Tests](https://img.shields.io/appveyor/tests/sentry/sentry-dotnet/main?compact_message)](https://ci.appveyor.com/project/sentry/sentry-dotnet/branch/main/tests)
[![codecov](https://codecov.io/gh/getsentry/sentry-dotnet/branch/main/graph/badge.svg)](https://codecov.io/gh/getsentry/sentry-dotnet)
[![Discord Chat](https://img.shields.io/discord/621778831602221064.svg)](https://discord.gg/Ww9hbqr)  


|      Integrations             |    Downloads     |    NuGet Stable     |    NuGet Preview     |
| ----------------------------- | :-------------------: | :-------------------: | :-------------------: |
|         **Sentry**            | [![Downloads](https://img.shields.io/nuget/dt/Sentry.svg)](https://www.nuget.org/packages/Sentry) | [![NuGet](https://img.shields.io/nuget/v/Sentry.svg)](https://www.nuget.org/packages/Sentry)   |    [![NuGet](https://img.shields.io/nuget/vpre/Sentry.svg)](https://www.nuget.org/packages/Sentry)   |
| **Sentry.Extensions.Logging** | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   |
|     **Sentry.AspNetCore**     | [![Downloads](https://img.shields.io/nuget/dt/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore) | [![NuGet](https://img.shields.io/nuget/v/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   |
|     **Sentry.AspNetCore.Grpc**     | [![Downloads](https://img.shields.io/nuget/dt/Sentry.AspNetCore.Grpc.svg)](https://www.nuget.org/packages/Sentry.AspNetCore.Grpc) | [![NuGet](https://img.shields.io/nuget/v/Sentry.AspNetCore.Grpc.svg)](https://www.nuget.org/packages/Sentry.AspNetCore.Grpc)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.Grpc.svg)](https://www.nuget.org/packages/Sentry.AspNetCore.Grpc)   |
|     **Sentry.AspNet**     | [![Downloads](https://img.shields.io/nuget/dt/Sentry.AspNet.svg)](https://www.nuget.org/packages/Sentry.AspNet) | [![NuGet](https://img.shields.io/nuget/v/Sentry.AspNet.svg)](https://www.nuget.org/packages/Sentry.AspNet)   |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNet.svg)](https://www.nuget.org/packages/Sentry.AspNet)   |
| **Sentry.Serilog**            | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Serilog.svg)](https://www.nuget.org/packages/Serilog) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Serilog.svg)](https://www.nuget.org/packages/Sentry.Serilog)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Serilog.svg)](https://www.nuget.org/packages/Sentry.Serilog)   |
| **Sentry.Log4Net**            | [![Downloads](https://img.shields.io/nuget/dt/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net) | [![NuGet](https://img.shields.io/nuget/v/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net)   |
| **Sentry.NLog**               | [![Downloads](https://img.shields.io/nuget/dt/Sentry.NLog.svg)](https://www.nuget.org/packages/Sentry.NLog) | [![NuGet](https://img.shields.io/nuget/v/Sentry.NLog.svg)](https://www.nuget.org/packages/Sentry.NLog)   | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.NLog.svg)](https://www.nuget.org/packages/Sentry.NLog)   |


## Documentation

Below you will find a basic introduction to the SDK and its API. 

For more details, please: refer to the [SDK](https://docs.sentry.io/quickstart/?platform=csharp) or [API](https://getsentry.github.io/sentry-dotnet/index.html) Documentation.
Looking for samples using the NuGet packages? Check out [this repository](https://github.com/getsentry/examples).

## Usage

**Consider taking a look at the _[samples](https://github.com/getsentry/sentry-dotnet/tree/main/samples)_ directory for different types of apps and example usages of the SDK.**

This SDK provides integrations which can hook into your app and automatically capture errors and context.

### Basic usage without framework integration

You can still use the SDK directly to send events to Sentry.
The integrations are just wrappers around the main SDK `Sentry`.

There's a [basic sample](https://github.com/getsentry/sentry-dotnet/blob/main/samples/Sentry.Samples.Console.Basic/Program.cs) and a one demonstrating [more customization](https://github.com/getsentry/sentry-dotnet/blob/main/samples/Sentry.Samples.Console.Customized/Program.cs).

Install the main SDK:
```shell
dotnet add package Sentry
```

Initialize the SDK:
```csharp
void Main() 
{
    using (SentrySdk.Init("dsn"))
    {
        // App code
    }
}
```
The SDK by default will watch for unhandled exceptions in your app.
If the [DSN](https://docs.sentry.io/quickstart/#configure-the-dsn) is not explicitly passed by parameter to `Init`, the SDK will try to locate it via environment variable `SENTRY_DSN`.

To configure advanced settings, for example a proxy server:
```csharp
void Main() 
{
    using (SentrySdk.Init(o =>
    {
        o.Dsn = "dsn";
        o.Proxy = new WebProxy("https://localhost:3128");
    }))
    {
        // App code
    }
}
```

Capture an exception:
```csharp
try
{
    throw null;
}
catch (Exception e)
{
    SentrySdk.CaptureException(e);
}
```


## ASP.NET Core integration

To use Sentry with your ASP.NET Core project, simply install the NuGet package:

```shell
dotnet add package Sentry.AspNetCore
```

Change your `Program.cs` by adding `UseSentry`:

```csharp
public static IWebHost BuildWebHost(string[] args) =>
    WebHost.CreateDefaultBuilder(args)
        .UseStartup<Startup>()
        // Integration
        .UseSentry()
        .Build();
```

### Logging integration

This will also automatically include the integration to `Microsoft.Extensions.Logging`. That means that any `LogError` or `LogCritical` by default will send an event to Sentry.

Log messages of level `Information` will be kept as _breadcrumbs_ and if an event is sent, all breadcrumbs from that transaction are included.

These levels can be configured so that the level you define tracks breadcrumbs or sends events or completely disable it.

**That means that log messages logged by you or the framework, related to the failed transaction, will be added to the event!**

## DSN

The SDK needs to know which project within Sentry your errors should go to. That's defined via the [DSN](https://docs.sentry.io/quickstart/#configure-the-dsn). You can provide it directly as an argument to `UseSentry`, defined via configuration like `appsettings.json` or set via environment variable `SENTRY_DSN`.
[This sample demonstrates defining the DSN via `appsettings.json`](https://github.com/getsentry/sentry-dotnet/blob/f7f5c8cafcf2a54ccffeedb9ed0359c880b6aae5/samples/Sentry.Samples.AspNetCore.Mvc/appsettings.json#L6).

## Configuration

The SDK is configurable, many of the settings are demonstrated through the samples but here are some options:

* HTTP Proxy
* Event sampling
* Filter exception by type
* Enable request body extraction
* Send PII data (Personal Identifiable Information, requires opt-in)
* Read [diagnostics activity data]("https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md)
* BeforeSend: Callback to modify/reject event before sending
* BeforeBreadcrumb: Callback to modify/reject a breadcrumb
* LogEventFilter: Filter events by inspecting log data
* Maximum number of breadcrumbs to store
* Event queue depth
* Shutdown timeout: If there are events to send, how long to wait until shutdown
* Accept compressed response
* Compress request body
* Breadcrumb level: Minimum log level to store as a breadcrumb
* Event level: Minimum log level to send an event to Sentry
* Disable duplicate event detection
* Disable capture of global unhandled exceptions
* Add event processor
* Add exception processor
* Enable SDK debug mode
* Attach stack trace for captured messages (opt-in)

and more...

## Microsoft.Extensions.Logging

If you want only the logging integration:
```shell
dotnet add package Sentry.Extensions.Logging
```
See the [logging integration only sample](https://github.com/getsentry/sentry-dotnet/blob/main/samples/Sentry.Samples.ME.Logging/Program.cs)


### Internals/Testability

It's often the case we don't want to couple our code with static class like `SentrySdk`, especially to allow our code to be testable.
If that's your case, you can use 2 abstractions:

* ISentryClient
* IHub

The `ISentryClient` is responsible to queueing the event to be sent to Sentry and abstracting away the internal transport.
The `IHub` on the other hand, holds a client and the current scope. It in fact also implements `ISentryClient` and is able to dispatch calls to the right client depending on the current scope.

In order to allow different events hold different contextual data, you need to know in which scope you are in.
That's the job of the `Hub`. It holds the scope management as well as a client. 

If all you are doing is sending events, without modification/access to the current scope, then you depend on `ISentryClient`. If on the other hand you would like to have access to the current scope by configuring it or binding a different client to it, etc. You'd depend on `IHub`.


An example using `IHub` for testability is [SentryLogger](https://github.com/getsentry/sentry-dotnet/blob/main/src/Sentry.Extensions.Logging/SentryLogger.cs) and its unit tests [SentryLoggerTests](https://github.com/getsentry/sentry-dotnet/blob/main/test/Sentry.Extensions.Logging.Tests/SentryLoggerTests.cs).  
`SentryLogger` depends on `IHub` because it does modify the scope (through `AddBreadcrumb`). In case it only sent events, it should instead depend on `ISentryClient`

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

Sentry's [Raven SDK](https://github.com/getsentry/raven-csharp/), battle tested with over 2.500.000 downloads on NuGet has support to .NET Framework 3.5+.

## Resources

* [![Documentation](https://img.shields.io/badge/documentation-sentry.io-green.svg)](https://docs.sentry.io/platforms/dotnet/)
* [![Forum](https://img.shields.io/badge/forum-sentry-green.svg)](https://forum.sentry.io/c/sdks)
* [![Discord](https://img.shields.io/discord/621778831602221064)](https://discord.gg/Ww9hbqr)
* [![Stack Overflow](https://img.shields.io/badge/stack%20overflow-sentry-green.svg)](http://stackoverflow.com/questions/tagged/sentry)
* [![Twitter Follow](https://img.shields.io/twitter/follow/getsentry?label=getsentry&style=social)](https://twitter.com/intent/follow?screen_name=getsentry)
