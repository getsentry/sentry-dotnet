# Proposed Documentation Changes for getsentry/sentry-docs

These files are intended to be applied to the [getsentry/sentry-docs](https://github.com/getsentry/sentry-docs) repository.
The file paths mirror the sentry-docs repo structure. Once the changes are applied to sentry-docs, this directory should be removed from sentry-dotnet.

## Context

Fixes https://github.com/getsentry/sentry-dotnet/issues/4898

The distributed tracing "how-to-use" docs for .NET were showing ASP.NET Framework-specific
content (`Application_BeginRequest`) on all .NET guide pages, including ASP.NET Core and
Blazor WebAssembly — which was incorrect.

## Changes

### Modified
- `platform-includes/distributed-tracing/how-to-use/dotnet.mdx` — Updated generic .NET
  fallback to focus on `SentryHttpMessageHandler` usage and DI-based auto-registration.

### New Files
- `platform-includes/distributed-tracing/how-to-use/dotnet.aspnetcore.mdx` — ASP.NET Core
  guide explaining automatic middleware trace extraction and `IHttpClientFactory` integration.
- `platform-includes/distributed-tracing/how-to-use/dotnet.blazor-webassembly.mdx` — Blazor
  WASM guide showing how to configure `SentryHttpMessageHandler` with `HttpClient`.
- `platform-includes/distributed-tracing/how-to-use/dotnet.aspnet.mdx` — Preserves the old
  ASP.NET Framework content for the correct guide page.
- `platform-includes/distributed-tracing/how-to-use/dotnet.azure-functions-worker.mdx` —
  Azure Functions Worker guide.

All includes also document the `TracePropagationTargets` option for controlling where
trace headers are sent.
