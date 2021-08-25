<p align="center">
  <a href="https://sentry.io" target="_blank">
    <img src="https://raw.githubusercontent.com/getsentry/sentry-dotnet/main/.assets/sentry-logo.png" alt="Sentry logo" width="280">
  </a>
</p>

_Bad software is everywhere, and we're tired of it. Sentry is on a mission to help developers write better software faster, so we can get back to enjoying technology. If you want to join us [<kbd>**Check out our open positions**</kbd>](https://sentry.io/careers/)_

Sentry Diagnostic Source Adds additional logging capabilities to the main SDK, such as, transactions with richer contexts, including database measurements, by integrating Entity Framework Core and SQLClient.

### About Sentry Diagnostic source

This package will automatically enable and set it up if your application runs on .NET 5 or greater. It'll also be enabled by default if you are using Sentry's ASP.NET and ASP.NET Core SDK.

In case of your application doesn't use any of the ASP.NET SDKs and relies on a .NET version older than .NET 5, you'll be able to use this integration by including its nuget package in your project and enabling it during Sentry's initialization.

```csharp
using Sentry;

SentrySdk.Init( option => {
    option.Dsn = "YOUR_DSN";
    option.AddDiagnosticListeners(); //Enables the diagnostic source integration.
});

```
NOTE: This setup is not required if your project targets .NET 5 or Greater and if your project includes Sentry.ASP.NET or Sentry.ASP.NET.Core Nuget.

## Disabling

You can easily disable it during the Sentry's Initialization by calling the SentryOption's extension  DisableDiagnosticListenerIntegration:
```csharp
using Sentry;

SentrySdk.Init( option => {
    option.Dsn = "YOUR_DSN";
    option.DisableDiagnosticListenerIntegration();
});
```

### When shouldn't I include this package

You will not need to include this package into your project if your project matches one of the following criteria:

* Your project Targets .NET 5 or greater.
* Your project includes the nuget [Sentry.AspNetCore](https://www.nuget.org/packages/Sentry.AspNetCore) version 3.8.4 or greater.
* Your project includes the nuget [Sentry.AspNet](https://www.nuget.org/packages/Sentry.AspNet) version 3.8.4 or greater.


### Screenshots

![Transaction with database events that came from the Diagnostic Source integration](".assets/transaction_with_ds_integration.png")
![Query compiler span](".assets/db_query_compiler.png")
![Query](".assets/db_query.png")
