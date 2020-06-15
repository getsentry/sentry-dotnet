# Getting started

It's possible to simply import a single NuGet package and integrate Sentry with pretty much **any** .NET application.

That is not necessarily the easiest way.

Sentry provides multiple **integrations**, for specific types of applications. It's advised to look for an integration that best fits your application.

## Main concepts:

## Sentry Client

The @Sentry.SentryClient is used to send events to Sentry. It has only synchronous methods because all its operations are executed in the calling thread without any I/O operation.

Calling @Sentry.SentrySdk.CaptureException(Exception) will create a @Sentry.SentryEvent from the @System.Exception provided. Internally, @Sentry.ISentryClient.CaptureEvent(Sentry.SentryEvent,Sentry.Protocol.Scope) is then called.

Calling @Sentry.ISentryClient.CaptureEvent(Sentry.SentryEvent,Sentry.Protocol.Scope) will prepare the @Sentry.SentryEvent, applying the current @Sentry.Protocol.Scope data to it if one exists. If any @Sentry.Extensibility.ISentryEventProcessor or @Sentry.Extensibility.ISentryEventExceptionProcessor was configured by you, those are invoked too.

Finally, the event is put into a in-memory queue to be sent to Sentry.

## Scope management

By default, any call to `AddBreadcrumb` or `ConfigureScope` will access the **same** shared scope throughout the app.

Perhaps that is what you need, for example on a WPF, WinForms or Xamarin app where a single user is using it. Or maybe you are building a ASP.NET application in which case you would prefer to create a **new scope per request**, ensuring that data in any single scope relates to a single request. 

The scope feature is leveraged by the [ASP.NET Core integration](https://github.com/getsentry/sentry-dotnet/tree/main/src/Sentry.AspNetCore) for exactly this reason. It isolates data from each request so in case an event happens, only relevant data is sent to Sentry. This means you don't need to dig through logs with correlation ids in order to find the data relevant to you.

Please check the [manual](~/manual/manual.md) for more.
