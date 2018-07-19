<p align="center">
  <a href="https://sentry.io" target="_blank" align="center">
    <img src="https://sentry-brand.storage.googleapis.com/sentry-logo-black.png" width="280">
  </a>
  <br />
</p>

## Entity Framework 6 Integration for Sentry

This is packages extend [Sentry's .NET SDK](https://github.com/getsentry/sentry-dotnet) with Entity Framework 6 queries as *Breadcrumb*s.
It also processes `DbEntityValidationException`s to extract the validation errors and add to the *Extra* field.
This increases the debuggability of Entity Framework related errors gratefully.

===========


|      Name                 |        NuGet         |
| ----------------------------- | -------------------: |
|     **Sentry.EntityFramework**     |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.EntityFramework.svg)](https://www.nuget.org/packages/Sentry.EntityFramework)   |

![Example in Sentry](.assets/ef.PNG)


## Usage

There are 2 steps to adding Entity Framework 6 support to your project:

* Call `SentryDatabaseLogging.UseBreadcrumbs()` to either your application's startup method, or into a static constructor inside your Entity Framework object. Make sure you only call this method once! This will add the interceptor to Entity Framework to log database queries.
* When setting up your `SentryClient`, use `SentryOptions.AddEntityFramework()`. This extension method will register all error processors to extract extra data, such as validation errors, from the exceptions thrown by Entity Framework.

## Samples

You may find a usage sample using ASP.NET MVC 5 under `/samples/Sentry.Samples.AspNet.Mvc`
