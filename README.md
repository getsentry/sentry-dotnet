<p align="center">
  <a href="https://sentry.io" target="_blank" align="center">
    <img src="https://sentry-brand.storage.googleapis.com/sentry-logo-black.png" width="280">
  </a>
  <br />
</p>

Work in progress, for a new .NET SDK
===========
[![Travis](https://travis-ci.org/getsentry/sentry-dotnet.svg?branch=master)](https://travis-ci.org/getsentry/sentry-dotnet)
[![AppVeyor](https://ci.appveyor.com/api/projects/status/wu055n0n4u8p20p2/branch/bootstrap?svg=true)](https://ci.appveyor.com/project/sentry/sentry-dotnet/branch/master)
[![codecov](https://codecov.io/gh/getsentry/sentry-dotnet/branch/master/graph/badge.svg)](https://codecov.io/gh/getsentry/sentry-dotnet)



NOTE: This repository is a work in progress. Our goal is to build a composable SDK with many integrations.

## Usage

** Consider taking a look at the `samples` directory for different types of apps and example usages of the SDK. **

The goal of this SDK is to provide integrations which can hook into your app and automatically capture errors and context. See ASP.NET Core below as an example.

You can still use the SDK directly to send events to Sentry:

Install the main SDK:
```shell
dotnet add package Sentry
```

Initialize the SDK:
```csharp
void Main() 
{
    using (SentrySdk.Init("https://id@sentry.io/project"))
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
        o.Dsn = new Dsn("https://id@sentry.io/project");
        o.Http(h =>
        {
            h.Proxy = new WebProxy("https://localhost:3128");
        });
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

Like:
```csharp
public static IWebHost BuildWebHost(string[] args) =>
    WebHost.CreateDefaultBuilder(args)
        .UseStartup<Startup>()
        // Integration
        .UseSentry("https://id@sentry.io/project")
        .Build();
```

This will also include automatically integration to `Microsoft.Extensions.Logging`. That means that any `LogError` or `LogCritical` by default will send an event to Sentry.
Log messages of level `Information` will be kept as _breadcrumbs_ and if an event is sent, all breadcrumbs from that transaction are included.

These levels can be configured so that the level you define tracks breadcrumbs or sends events or completely disable it.

## Get involved
Join the discussion in our
[tracking issue](https://github.com/getsentry/sentry-dotnet/issues/1) and let us
know what you think of the new API and new features.

## Resources
* [![Gitter chat](https://img.shields.io/gitter/room/getsentry/dotnet.svg)](https://gitter.im/getsentry/dotnet)
* [![Stack Overflow](https://img.shields.io/badge/stack%20overflow-sentry-green.svg)](http://stackoverflow.com/questions/tagged/sentry)
* [![Forum](https://img.shields.io/badge/forum-sentry-green.svg)](https://forum.sentry.io/c/sdks)
* Follow [@getsentry](https://twitter.com/getsentry) on Twitter for updates
