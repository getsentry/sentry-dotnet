# Changelog

## 6.1.0

### Features

- Add _experimental_ support for [Sentry trace-connected Metrics](https://docs.sentry.io/product/explore/metrics/) ([#4834](https://github.com/getsentry/sentry-dotnet/pull/4834))
- Extended `SentryThread` by `Main` to allow indication whether the thread is considered the current main thread ([#4807](https://github.com/getsentry/sentry-dotnet/pull/4807))

### Fixes

- User Feedback now contains additional Context and Tags, like `Environment` and `Release` ([#4883](https://github.com/getsentry/sentry-dotnet/pull/4883))
- Allow Sentry failures from the Sentry CLI when SENTRY_ALLOW_FAILURE is set ([#4852](https://github.com/getsentry/sentry-dotnet/pull/4852))
- The SDK now logs a specific error message when envelopes are rejected due to size limits (HTTP 413) ([#4863](https://github.com/getsentry/sentry-dotnet/pull/4863))
- Fixed thread-safety issue on Android when multiple events are captured concurrently ([#4814](https://github.com/getsentry/sentry-dotnet/pull/4814))

### Dependencies

- Bumped Xamarin.AndroidX.Lifecycle.Common.Java8 to 2.2.20 ([#4876](https://github.com/getsentry/sentry-dotnet/pull/4876))
- Bumped CommunityToolkit.Mvvm to 8.4.0 ([#4876](https://github.com/getsentry/sentry-dotnet/pull/4876))
- Bump Native SDK from v0.12.2 to v0.12.4 ([#4832](https://github.com/getsentry/sentry-dotnet/pull/4832), [#4875](https://github.com/getsentry/sentry-dotnet/pull/4875))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#0124)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.12.2...0.12.4)
- Bump Native SDK from v0.12.2 to v0.12.6 ([#4832](https://github.com/getsentry/sentry-dotnet/pull/4832), [#4875](https://github.com/getsentry/sentry-dotnet/pull/4875), [#4892](https://github.com/getsentry/sentry-dotnet/pull/4892), [#4897](https://github.com/getsentry/sentry-dotnet/pull/4897))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#0126)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.12.2...0.12.6)
- Bump Java SDK from v8.28.0 to v8.29.0 ([#4817](https://github.com/getsentry/sentry-dotnet/pull/4817))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#8290)
  - [diff](https://github.com/getsentry/sentry-java/compare/8.28.0...8.29.0)

## 6.0.0

### BREAKING CHANGES

- This release adds support for .NET 10 and drops support for net8.0-android, net8.0-ios, net8.0-maccatalyst and net8.0-windows10.0.19041.0 ([#4461](https://github.com/getsentry/sentry-dotnet/pull/4461))
- Backpressure handling is now enabled by default, meaning that the SDK will monitor system health and reduce the sampling rate of events and transactions when the system is under load. When the system is determined to be healthy again, the sampling rates are returned to their original levels. ([#4615](https://github.com/getsentry/sentry-dotnet/pull/4615))
- Remove `SentryLoggingOptions.ExperimentalLogging.MinimumLogLevel`. _Structured Logs_ can now be configured via the `"Sentry"` logging provider (e.g. in `appsettings.json` and `appsettings.{HostEnvironment}.json`) ([#4700](https://github.com/getsentry/sentry-dotnet/pull/4700))
- All logging provider types are _internal_ now in order to ensure configuration as intended ([#4700](https://github.com/getsentry/sentry-dotnet/pull/4700))
- Rename `SentryLog.ParentSpanId` to `SentryLog.SpanId` reflecting the protocol change ([#4778](https://github.com/getsentry/sentry-dotnet/pull/4778))
- QOL improvement: Spans and Transactions now implement `IDisposable` so that they can be used with `using` statements/declarations that will automatically finish the span with a status of OK when it passes out of scope, if it has not already been finished, to be consistent with `Activity` classes when using OpenTelemetry ([#4627](https://github.com/getsentry/sentry-dotnet/pull/4627))
- SpanTracer and TransactionTracer are still public but these are now `sealed` (see also [#4627](https://github.com/getsentry/sentry-dotnet/pull/4627))
- The _Structured Logs_ APIs are now stable: removed `Experimental` from `SentryOptions` ([#4699](https://github.com/getsentry/sentry-dotnet/pull/4699))
- Added support for v3 of the Android AssemblyStore format that is used in .NET 10 and dropped support for v1 that was used in .NET 8 ([#4583](https://github.com/getsentry/sentry-dotnet/pull/4583))
- CaptureFeedback now returns a `SentryId` and a `CaptureFeedbackResult` out parameter that indicate whether feedback was captured successfully and what the reason for failure was otherwise ([#4613](https://github.com/getsentry/sentry-dotnet/pull/4613))
- Deprecated `Sentry.Azure.Functions.Worker` as very few people were using it and the functionality can easily be replaced with OpenTelemetry. We've replaced our integration with a sample showing how to do this using our OpenTelemetry package instead. ([#4693](https://github.com/getsentry/sentry-dotnet/pull/4693))
- UWP support has been dropped. Future efforts will likely focus on WinUI 3, in line with Microsoft's recommendations for building Windows UI apps. ([#4686](https://github.com/getsentry/sentry-dotnet/pull/4686))
- `BreadcrumbLevel.Critical` has been renamed to `BreadcrumbLevel.Fatal` for consistency with the other Sentry SDKs ([#4605](https://github.com/getsentry/sentry-dotnet/pull/4605))
- SentryOptions.IsEnvironmentUser now defaults to false on MAUI. The means the User.Name will no longer be set, by default, to the name of the device ([#4606](https://github.com/getsentry/sentry-dotnet/pull/4606))
- Removed obsolete APIs ([#4619](https://github.com/getsentry/sentry-dotnet/pull/4619))
  - Removed the unusual constructor from `Sentry.Maui.BreadcrumbEvent` that had been marked as obsolete. That constructor expected a `IEnumerable<(string Key, string Value)>[]` argument (i.e. an array of IEnumerable of tuples). If you were using this constructor, you should instead use the alternate constructor that expects just an IEnumerable of tuples: `IEnumerable<(string Key, string Value)>`.
  - Removed `SentrySdk.CaptureUserFeedback` and all associated members. Use the newer `SentrySdk.CaptureFeedback` instead.
- ScopeExtensions.Populate is now internal ([#4611](https://github.com/getsentry/sentry-dotnet/pull/4611))

### Features

- Support for .NET 10 ([#4461](https://github.com/getsentry/sentry-dotnet/pull/4461))
- Added a new SDK `Sentry.Extensions.AI` which allows LLM usage instrumentation via `Microsoft.Extensions.AI` ([#4657](https://github.com/getsentry/sentry-dotnet/pull/4657))
- Added experimental support for Session Replay on iOS ([#4664](https://github.com/getsentry/sentry-dotnet/pull/4664))
- Add support for _Structured Logs_ in `Sentry.Google.Cloud.Functions` ([#4700](https://github.com/getsentry/sentry-dotnet/pull/4700))
- QOL features for Unity
  - The SDK now provides a `IsSessionActive` to allow checking the session state ([#4662](https://github.com/getsentry/sentry-dotnet/pull/4662))
  - The SDK now makes use of the new SessionEndStatus `Unhandled` when capturing an unhandled but non-terminal exception, i.e. through the UnobservedTaskExceptionIntegration ([#4633](https://github.com/getsentry/sentry-dotnet/pull/4633), [#4653](https://github.com/getsentry/sentry-dotnet/pull/4653))
- Extended the App context by `app_memory` that can hold the amount of memory used by the application in bytes. ([#4707](https://github.com/getsentry/sentry-dotnet/pull/4707))
- Add support for W3C traceparent header for outgoing requests ([#4661](https://github.com/getsentry/sentry-dotnet/pull/4661))
  - This feature is disabled by default. Set `PropagateTraceparent = true` when initializing the SDK if to include the W3C traceparent header on outgoing requests.
  - See https://develop.sentry.dev/sdk/telemetry/traces/distributed-tracing/#w3c-trace-context-header for more details.

### Fixes

- Memory leak when finishing an unsampled Transaction that has started unsampled Spans ([#4717](https://github.com/getsentry/sentry-dotnet/pull/4717))
- Sentry Tracing middleware crashed ASP.NET Core in .NET 10 in 6.0.0-rc.1 and earlier ([#4747](https://github.com/getsentry/sentry-dotnet/pull/4747))
- Captured [Http Client Errors](https://docs.sentry.io/platforms/dotnet/guides/aspnet/configuration/http-client-errors/) on .NET 5+ now include a full stack trace in order to improve Issue grouping ([#4724](https://github.com/getsentry/sentry-dotnet/pull/4724))
- Deliver system breadcrumbs in the main thread on Android ([#4671](https://github.com/getsentry/sentry-dotnet/pull/4671))
- The `Serilog` integration captures _Structured Logs_ (when enabled) independently of captured Events and added Breadcrumbs ([#4691](https://github.com/getsentry/sentry-dotnet/pull/4691))
- Minimum Log-Level for _Structured Logs_, _Breadcrumbs_ and _Events_ in all Logging-Integrations ([#4700](https://github.com/getsentry/sentry-dotnet/pull/4700))
  - for `Sentry.Extensions.Logging`, `Sentry.AspNetCore`, `Sentry.Maui` and `Sentry.Google.Cloud.Functions`
  - the Logger-Provider for _Breadcrumbs_ and _Events_ ignores Logging-Configuration (e.g. via `appsettings.json`)
    - use the intended `SentryLoggingOptions.MinimumBreadcrumbLevel`, `SentryLoggingOptions.MinimumEventLevel`, or add filter functions via `SentryLoggingOptionsExtensions.AddLogEntryFilter`
  - the Logger-Provider for _Structured Logs_ respects Logging-Configuration (e.g. via `appsettings.json`)
    - when enabled by `SentryOptions.EnableLogs`
- Avoid appending `/NODEFAULTLIB:MSVCRT` to NativeAOT linker arguments on Windows when targetting non-Windows platforms (Android, Browser) ([#4760](https://github.com/getsentry/sentry-dotnet/pull/4760))
- The SDK avoids redundant scope sync after transaction finish ([#4623](https://github.com/getsentry/sentry-dotnet/pull/4623))
- sentry-native is now automatically disabled for WASM applications ([#4631](https://github.com/getsentry/sentry-dotnet/pull/4631))
- Remove unnecessary files from SentryCocoaFramework before packing ([#4602](https://github.com/getsentry/sentry-dotnet/pull/4602))

### Dependencies

- Bump Java SDK from v8.24.0 to v8.28.0 ([#4728](https://github.com/getsentry/sentry-dotnet/pull/4728), [#4761](https://github.com/getsentry/sentry-dotnet/pull/4761), [#4791](https://github.com/getsentry/sentry-dotnet/pull/4791))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#8280)
  - [diff](https://github.com/getsentry/sentry-java/compare/8.24.0...8.28.0)
- Bump Native SDK from v0.12.0 to v0.12.2 ([#4690](https://github.com/getsentry/sentry-dotnet/pull/4690), [#4737](https://github.com/getsentry/sentry-dotnet/pull/4737), [#4780](https://github.com/getsentry/sentry-dotnet/pull/4780))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#0122)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.12.0...0.12.2)
- Bump Cocoa SDK from v8.57.1 to v8.57.3 ([#4704](https://github.com/getsentry/sentry-dotnet/pull/4704), [#4738](https://github.com/getsentry/sentry-dotnet/pull/4738))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8573)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.57.1...8.57.3)
- Bump CLI from v2.57.0 to v2.58.2 ([#4705](https://github.com/getsentry/sentry-dotnet/pull/4705), [#4727](https://github.com/getsentry/sentry-dotnet/pull/4727), [#4732](https://github.com/getsentry/sentry-dotnet/pull/4732))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2582)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.57.0...2.58.2)

## 5.16.2

### Fixes

- Do not allow multiple `sentry.proguard-uuid` metadata to be set in Android manifest ([#4647](https://github.com/getsentry/sentry-dotnet/pull/4647))

### Dependencies

- Bump Cocoa SDK from v8.56.2 to v8.57.1 ([#4637](https://github.com/getsentry/sentry-dotnet/pull/4637), [#4680](https://github.com/getsentry/sentry-dotnet/pull/4680))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8571)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.56.2...8.57.1)
- Bump Native SDK from v0.11.2 to v0.12.0 ([#4636](https://github.com/getsentry/sentry-dotnet/pull/4636), [#4678](https://github.com/getsentry/sentry-dotnet/pull/4678))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#0120)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.11.2...0.12.0)
- Bump Java SDK from v8.23.0 to v8.24.0 ([#4667](https://github.com/getsentry/sentry-dotnet/pull/4667))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#8240)
  - [diff](https://github.com/getsentry/sentry-java/compare/8.23.0...8.24.0)
- Bump CLI from v2.56.1 to v2.57.0 ([#4668](https://github.com/getsentry/sentry-dotnet/pull/4668))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2570)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.56.1...2.57.0)

## 5.16.1

### Fixes

- Structured Logs now have a `sentry.origin` attribute to so it's clearer where these come from ([#4566](https://github.com/getsentry/sentry-dotnet/pull/4566))

### Dependencies

- Bump Java SDK from v8.22.0 to v8.23.0 ([#4586](https://github.com/getsentry/sentry-dotnet/pull/4586))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#8230)
  - [diff](https://github.com/getsentry/sentry-java/compare/8.22.0...8.23.0)
- Bump Native SDK from v0.11.1 to v0.11.2 ([#4590](https://github.com/getsentry/sentry-dotnet/pull/4590))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#0112)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.11.1...0.11.2)
- Bump CLI from v2.56.0 to v2.56.1 ([#4625](https://github.com/getsentry/sentry-dotnet/pull/4625))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2561)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.56.0...2.56.1)

## 5.16.0

### Features

- Added `EnableBackpressureHandling` option for Automatic backpressure handling. When enabled this automatically reduces the sample rate when the SDK detects events being dropped. ([#4452](https://github.com/getsentry/sentry-dotnet/pull/4452))
- Add (experimental) _Structured Logs_ integration for `Serilog` ([#4462](https://github.com/getsentry/sentry-dotnet/pull/4462))

### Fixes

- Templates are no longer sent with Structured Logs that have no parameters ([#4544](https://github.com/getsentry/sentry-dotnet/pull/4544))
- Parent-Span-IDs are no longer sent with Structured Logs when recorded without an active Span ([#4565](https://github.com/getsentry/sentry-dotnet/pull/4565))
- Upload linked PDBs to fix non-IL-stripped symbolication for iOS ([#4527](https://github.com/getsentry/sentry-dotnet/pull/4527))
- In MAUI Android apps, generate and inject UUID to APK and upload ProGuard mapping to Sentry with the UUID ([#4532](https://github.com/getsentry/sentry-dotnet/pull/4532))
- Fixed WASM0001 warning when building Blazor WebAssembly projects ([#4519](https://github.com/getsentry/sentry-dotnet/pull/4519))

### API Changes

- Remove `ExperimentalAttribute` from all _Structured Logs_ APIs, and remove `Experimental` property from `SentrySdk`, but keep `Experimental` property on `SentryOptions` ([#4567](https://github.com/getsentry/sentry-dotnet/pull/4567))

### Dependencies

- Bump Cocoa SDK from v8.56.0 to v8.56.2 ([#4555](https://github.com/getsentry/sentry-dotnet/pull/4555), [#4572](https://github.com/getsentry/sentry-dotnet/pull/4572))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8562)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.56.0...8.56.2)
- Bump Native SDK from v0.11.0 to v0.11.1 ([#4557](https://github.com/getsentry/sentry-dotnet/pull/4557))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#0111)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.11.0...0.11.1)
- Bump CLI from v2.54.0 to v2.55.0 ([#4556](https://github.com/getsentry/sentry-dotnet/pull/4556))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2550)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.54.0...2.55.0)

### Dependencies

- Bump CLI from v2.54.0 to v2.56.0 ([#4556](https://github.com/getsentry/sentry-dotnet/pull/4556), [#4577](https://github.com/getsentry/sentry-dotnet/pull/4577))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2560)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.54.0...2.56.0)
- Bump Java SDK from v8.21.1 to v8.22.0 ([#4552](https://github.com/getsentry/sentry-dotnet/pull/4552))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#8220)
  - [diff](https://github.com/getsentry/sentry-java/compare/8.21.1...8.22.0)

## 5.15.1

### Fixes

- Fail when building Blazor WASM with Profiling. We don't support profiling in Blazor WebAssembly projects. ([#4512](https://github.com/getsentry/sentry-dotnet/pull/4512))
- Do not overwrite user IP if it is set manually in ASP.NET sdk ([#4513](https://github.com/getsentry/sentry-dotnet/pull/4513))
- Fix `SentryOptions.Native.SuppressSignalAborts` and `SuppressExcBadAccess` on iOS ([#4521](https://github.com/getsentry/sentry-dotnet/pull/4521))

### Dependencies

- Bump Cocoa SDK from v8.55.1 to v8.56.0 ([#4528](https://github.com/getsentry/sentry-dotnet/pull/4528))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8560)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.55.1...8.56.0)
- Bump CLI from v2.53.0 to v2.54.0 ([#4541](https://github.com/getsentry/sentry-dotnet/pull/4541))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2540)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.53.0...2.54.0)
- Bump Native SDK from v0.10.1 to v0.11.0 ([#4542](https://github.com/getsentry/sentry-dotnet/pull/4542))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#0110)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.10.1...0.11.0)

## 5.15.0

### Features

- Experimental _Structured Logs_:
  - Redesign SDK Logger APIs to allow usage of `params` ([#4451](https://github.com/getsentry/sentry-dotnet/pull/4451))
  - Shorten the `key` names of `Microsoft.Extensions.Logging` attributes ([#4450](https://github.com/getsentry/sentry-dotnet/pull/4450))

### Fixes

- Experimental _Structured Logs_:
  - Remove `IDisposable` from `SentryStructuredLogger`. Disposal is intended through the owning `IHub` instance ([#4424](https://github.com/getsentry/sentry-dotnet/pull/4424))
  - Ensure all buffered logs are sent to Sentry when the application terminates unexpectedly ([#4425](https://github.com/getsentry/sentry-dotnet/pull/4425))
  - `InvalidOperationException` potentially thrown during a race condition, especially in concurrent high-volume logging scenarios ([#4428](https://github.com/getsentry/sentry-dotnet/pull/4428))
- Blocking calls are no longer treated as unhandled crashes ([#4458](https://github.com/getsentry/sentry-dotnet/pull/4458))
- Only apply Session Replay masks to specific control types when necessary to avoid performance issues in MAUI apps with complex UIs ([#4445](https://github.com/getsentry/sentry-dotnet/pull/4445))
- De-duplicate Java.Lang.RuntimeException on Android ([#4509](https://github.com/getsentry/sentry-dotnet/pull/4509))
- Upload linked PDB to fix symbolication for Mac Catalyst ([#4503](https://github.com/getsentry/sentry-dotnet/pull/4503))

### Dependencies

- Bump sentry-cocoa from 8.39.0 to 8.55.1 ([#4442](https://github.com/getsentry/sentry-dotnet/pull/4442), [#4483](https://github.com/getsentry/sentry-dotnet/pull/4483), [#4485](https://github.com/getsentry/sentry-dotnet/pull/4485))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8551)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.39.0...8.55.1)
- Bump Native SDK from v0.9.1 to v0.10.1 ([#4436](https://github.com/getsentry/sentry-dotnet/pull/4436), [#4492](https://github.com/getsentry/sentry-dotnet/pull/4492))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#0101)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.9.1...0.10.1)
- Bump CLI from v2.52.0 to v2.53.0 ([#4486](https://github.com/getsentry/sentry-dotnet/pull/4486))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2530)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.52.0...2.53.0)
- Bump Java SDK from v8.6.0 to v8.21.1 ([#4496](https://github.com/getsentry/sentry-dotnet/pull/4496), [#4502](https://github.com/getsentry/sentry-dotnet/pull/4502), [#4508](https://github.com/getsentry/sentry-dotnet/pull/4508))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#8211)
  - [diff](https://github.com/getsentry/sentry-java/compare/8.6.0...8.21.1)

## 5.14.1

### Fixes

- Crontabs now support day names (MON-SUN) and allow step values and ranges to be combined ([#4407](https://github.com/getsentry/sentry-dotnet/pull/4407))
- Ensure the correct Sentry Cocoa SDK framework version is used on iOS ([#4411](https://github.com/getsentry/sentry-dotnet/pull/4411))

### Dependencies

- Bump CLI from v2.50.2 to v2.52.0 ([#4419](https://github.com/getsentry/sentry-dotnet/pull/4419), [#4435](https://github.com/getsentry/sentry-dotnet/pull/4435), [#4444](https://github.com/getsentry/sentry-dotnet/pull/4444))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2520)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.50.2...2.52.0)

## 5.14.0

### Features

- Add _experimental_ support for [Sentry Structured Logging](https://docs.sentry.io/product/explore/logs/) ([#4308](https://github.com/getsentry/sentry-dotnet/pull/4308))
  - Structured-Logger API ([#4158](https://github.com/getsentry/sentry-dotnet/pull/4158))
  - Buffering and Batching ([#4310](https://github.com/getsentry/sentry-dotnet/pull/4310))
  - Integrations for `Sentry.Extensions.Logging`, `Sentry.AspNetCore` and `Sentry.Maui` ([#4193](https://github.com/getsentry/sentry-dotnet/pull/4193))

### Fixes

- Update `sample_rate` of _Dynamic Sampling Context (DSC)_ when making sampling decisions ([#4374](https://github.com/getsentry/sentry-dotnet/pull/4374))

## 5.13.0

### Features

- Sentry now includes an EXPERIMENTAL StringStackTraceFactory. This factory isn't as feature rich as the full `SentryStackTraceFactory`. However, it may provide better results if you are compiling your application AOT and not getting useful stack traces from the full stack trace factory. ([#4362](https://github.com/getsentry/sentry-dotnet/pull/4362))

### Fixes

- Source context for class libraries when running on Android in Release mode ([#4294](https://github.com/getsentry/sentry-dotnet/pull/4294))
- Native AOT: don't load SentryNative on unsupported platforms ([#4347](https://github.com/getsentry/sentry-dotnet/pull/4347))
- Fixed issue introduced in release 5.12.0 that might prevent other middleware or user code from reading request bodies ([#4373](https://github.com/getsentry/sentry-dotnet/pull/4373))
- SentryTunnelMiddleware overwrites the X-Forwarded-For header ([#4375](https://github.com/getsentry/sentry-dotnet/pull/4375))
- Native AOT support for `linux-musl-arm64` ([#4365](https://github.com/getsentry/sentry-dotnet/pull/4365))

### Dependencies

- Bump CLI from v2.47.0 to v2.50.2 ([#4348](https://github.com/getsentry/sentry-dotnet/pull/4348), [#4370](https://github.com/getsentry/sentry-dotnet/pull/4370), [#4378](https://github.com/getsentry/sentry-dotnet/pull/4378))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2502)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.47.0...2.50.2)

## 5.12.0

### API changes

- App Hang Tracking for iOS is now disabled by default, until this functionality is more stable. If you want to use it in your applications then you'll need to enable this manually. ([#4320](https://github.com/getsentry/sentry-dotnet/pull/4320))

### Features

- Added StartSpan and GetTransaction methods to the SentrySdk ([#4303](https://github.com/getsentry/sentry-dotnet/pull/4303))

### Fixes

- Avoid double reporting sessions on iOS and Android apps ([#4341](https://github.com/getsentry/sentry-dotnet/pull/4341))
- Sentry now decompresses Request bodies in ASP.NET Core when RequestDecompression middleware is enabled ([#4315](https://github.com/getsentry/sentry-dotnet/pull/4315))
- Custom ISentryEventProcessors are now run for native iOS events ([#4318](https://github.com/getsentry/sentry-dotnet/pull/4318))
- Crontab validation when capturing checkins ([#4314](https://github.com/getsentry/sentry-dotnet/pull/4314))
- Fixed an issue with the way Sentry detects build settings. This was causing Sentry to produce code that could fail at runtime in AOT compiled applications. ([#4333](https://github.com/getsentry/sentry-dotnet/pull/4333))
- Native AOT: link to static `lzma` on Linux/MUSL ([#4326](https://github.com/getsentry/sentry-dotnet/pull/4326))
- AppDomain.CurrentDomain.ProcessExit hook is now removed on shutdown ([#4323](https://github.com/getsentry/sentry-dotnet/pull/4323))

### Dependencies

- Bump Native SDK from v0.9.0 to v0.9.1 ([#4309](https://github.com/getsentry/sentry-dotnet/pull/4309))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#091)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.9.0...0.9.1)
- Bump CLI from v2.46.0 to v2.47.0 ([#4332](https://github.com/getsentry/sentry-dotnet/pull/4332))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2470)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.46.0...2.47.0)

## 5.11.2

### Fixes

- Unsampled spans no longer propagate empty trace headers ([#4302](https://github.com/getsentry/sentry-dotnet/pull/4302))

## 5.11.1

### Fixes

- Fix linking of libsentry-native to avoid DllNotFoundException in Native AOT applications ([#4298](https://github.com/getsentry/sentry-dotnet/pull/4298))

## 5.11.0

### Features

- Added non-allocating `ConfigureScope` and `ConfigureScopeAsync` overloads ([#4244](https://github.com/getsentry/sentry-dotnet/pull/4244))
- Add .NET MAUI `AutomationId` element information to breadcrumbs ([#4248](https://github.com/getsentry/sentry-dotnet/pull/4248))
- The HTTP Response Status Code for spans instrumented using OpenTelemetry is now searchable ([#4283](https://github.com/getsentry/sentry-dotnet/pull/4283))

### Fixes

- The HTTP instrumentation uses the span created for the outgoing request in the sentry-trace header, fixing the parent-child relationship between client and server ([#4264](https://github.com/getsentry/sentry-dotnet/pull/4264))
- ExtraData not captured for Breadcrumbs in MauiEventsBinder ([#4254](https://github.com/getsentry/sentry-dotnet/pull/4254))
  - NOTE: Required breaking changes to the public API of `Sentry.Maui.BreadcrumbEvent`, while keeping an _Obsolete_ constructor for backward compatibility.
- InvalidOperationException sending attachments on Android with LLVM enabled ([#4276](https://github.com/getsentry/sentry-dotnet/pull/4276))
- When CaptureFeedback methods are called with invalid email addresses, the email address will be removed and, if Debug mode is enabled, a warning will be logged. This is done to avoid losing the Feedback altogether (Sentry would reject Feedback that has an invalid email address) ([#4284](https://github.com/getsentry/sentry-dotnet/pull/4284))

### Dependencies

- Bump the version of the .NET SDK that we use from 9.0.203 to 9.0.301 ([#4272](https://github.com/getsentry/sentry-dotnet/pull/4272))
  - Note that this also required we bump various Java dependencies (since version 9.0.300 of the Android workload requires newer versions of the these)
  - See https://docs.sentry.io/platforms/dotnet/troubleshooting/#detected-package-version-outside-of-dependency-constraint if you see NU1605, NU1608 and/or NU1107 warnings after upgrading
- Bump Native SDK from v0.8.5 to v0.9.0 ([#4260](https://github.com/getsentry/sentry-dotnet/pull/4260))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#090)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.8.5...0.9.0)

## 5.10.0

### Features

- Rename MemoryInfo.AllocatedBytes to MemoryInfo.TotalAllocatedBytes ([#4243](https://github.com/getsentry/sentry-dotnet/pull/4243))
- Replace libcurl with .NET HttpClient for sentry-native ([#4222](https://github.com/getsentry/sentry-dotnet/pull/4222))

### Fixes

- InvalidCastException in SentrySpanProcessor when using the Sentry.OpenTelemetry integration ([#4245](https://github.com/getsentry/sentry-dotnet/pull/4245))
- Fix InApp Exclude for frames without Module by checking against frame's Package ([#4236](https://github.com/getsentry/sentry-dotnet/pull/4236))

## 5.9.0

### Features

- Reduced memory pressure when sampling less than 100% of traces/transactions ([#4212](https://github.com/getsentry/sentry-dotnet/pull/4212))
- Add SentrySdk.SetTag ([#4232](https://github.com/getsentry/sentry-dotnet/pull/4232))

### Fixes

- Fixed symbolication for net9.0-android applications in Release config ([#4221](https://github.com/getsentry/sentry-dotnet/pull/4221))
- Support Linux arm64 on Native AOT ([#3700](https://github.com/getsentry/sentry-dotnet/pull/3700))
- Revert W3C traceparent support ([#4204](https://github.com/getsentry/sentry-dotnet/pull/4204))

### Dependencies

- Bump CLI from v2.45.0 to v2.46.0 ([#4226](https://github.com/getsentry/sentry-dotnet/pull/4226))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2460)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.45.0...2.46.0)

## 5.8.1

### Fixes

- Support musl on Linux ([#4188](https://github.com/getsentry/sentry-dotnet/pull/4188))
- Support for Windows ARM64 with Native AOT ([#4187](https://github.com/getsentry/sentry-dotnet/pull/4187))
- Addressed potential performance issue with Sentry.Maui ([#4219](https://github.com/getsentry/sentry-dotnet/pull/4219))
- Respect `SentryNative=false` at runtime ([#4220](https://github.com/getsentry/sentry-dotnet/pull/4220))

## 5.8.0

### Features

- .NET MAUI integration with CommunityToolkit.Mvvm Async Relay Commands can now be auto spanned with the new package Sentry.Maui.CommunityToolkit.Mvvm ([#4125](https://github.com/getsentry/sentry-dotnet/pull/4125))

### Fixes

- Revert "Bump Cocoa SDK from v8.39.0 to v8.46.0 (#4103)" ([#4202](https://github.com/getsentry/sentry-dotnet/pull/4202))
  - IMPORTANT: Fixes multiple issues running versions 5.6.x and 5.7.x of the Sentry SDK for .NET on iOS (initialising the SDK and sending data to Sentry)

### Dependencies

- Bump Native SDK from v0.8.4 to v0.8.5 ([#4189](https://github.com/getsentry/sentry-dotnet/pull/4189))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#085)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.8.4...0.8.5)

## 5.7.0

### Features

- New source generator allows Sentry to see true build variables like PublishAot and PublishTrimmed to properly adapt checks in the Sentry SDK ([#4101](https://github.com/getsentry/sentry-dotnet/pull/4101))
- Auto breadcrumbs now include all .NET MAUI gesture recognizer events ([#4124](https://github.com/getsentry/sentry-dotnet/pull/4124))
- Associate replays with errors and traces on Android ([#4133](https://github.com/getsentry/sentry-dotnet/pull/4133))

### Fixes

- Redact Authorization headers before sending events to Sentry ([#4164](https://github.com/getsentry/sentry-dotnet/pull/4164))
- Remove Strong Naming from Sentry.Hangfire ([#4099](https://github.com/getsentry/sentry-dotnet/pull/4099))
- Increase `RequestSize.Small` threshold from 1 kB to 4 kB to match other SDKs ([#4177](https://github.com/getsentry/sentry-dotnet/pull/4177))

### Dependencies

- Bump CLI from v2.43.1 to v2.45.0 ([#4169](https://github.com/getsentry/sentry-dotnet/pull/4169), [#4179](https://github.com/getsentry/sentry-dotnet/pull/4179))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2450)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.43.1...2.45.0)

## 5.7.0-beta.0

### Features

- When setting a transaction on the scope, the SDK will attempt to sync the transaction's trace context with the SDK on the native layer. Finishing a transaction will now also start a new trace ([#4153](https://github.com/getsentry/sentry-dotnet/pull/4153))
- Added `CaptureFeedback` overload with `configureScope` parameter ([#4073](https://github.com/getsentry/sentry-dotnet/pull/4073))
- Custom SessionReplay masks in MAUI Android apps ([#4121](https://github.com/getsentry/sentry-dotnet/pull/4121))

### Fixes

- Work around iOS SHA1 bug ([#4143](https://github.com/getsentry/sentry-dotnet/pull/4143))
- Prevent Auto Breadcrumbs Event Binder from leaking and rebinding events ([#4159](https://github.com/getsentry/sentry-dotnet/pull/4159))
- Fixes build error when building .NET Framework applications using Sentry 5.6.0: `MSB4185 :The function "IsWindows" on type "System.OperatingSystem" is not available` ([#4160](https://github.com/getsentry/sentry-dotnet/pull/4160))
- Added a `SentrySetCommitReleaseOptions` build property that can be specified separately from `SentryReleaseOptions` ([#4109](https://github.com/getsentry/sentry-dotnet/pull/4109))

### Dependencies

- Bump CLI from v2.43.0 to v2.43.1 ([#4151](https://github.com/getsentry/sentry-dotnet/pull/4151))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2431)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.43.0...2.43.1)

## 5.6.0

### Features

- Option to disable the SentryNative integration ([#4107](https://github.com/getsentry/sentry-dotnet/pull/4107), [#4134](https://github.com/getsentry/sentry-dotnet/pull/4134))
  - To disable it, add this msbuild property: `<SentryNative>false</SentryNative>`
- Reintroduced experimental support for Session Replay on Android ([#4097](https://github.com/getsentry/sentry-dotnet/pull/4097))
- If an incoming HTTP request has the `traceparent` header, it is now parsed and interpreted like the `sentry-trace` header. Outgoing requests now contain the `traceparent` header to facilitate integration with servesr that only support the [W3C Trace Context](https://www.w3.org/TR/trace-context/). ([#4084](https://github.com/getsentry/sentry-dotnet/pull/4084))

### Fixes

- Ensure user exception data is not removed by AspNetCoreExceptionProcessor ([#4016](https://github.com/getsentry/sentry-dotnet/pull/4106))
- Prevent users from disabling AndroidEnableAssemblyCompression which leads to untrappable crash ([#4089](https://github.com/getsentry/sentry-dotnet/pull/4089))
- Fixed MSVCRT build warning on Windows ([#4111](https://github.com/getsentry/sentry-dotnet/pull/4111))

### Dependencies

- Bump Cocoa SDK from v8.39.0 to v8.46.0 ([#4103](https://github.com/getsentry/sentry-dotnet/pull/4103))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8460)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.39.0...8.46.0)
- Bump Native SDK from v0.8.3 to v0.8.4 ([#4122](https://github.com/getsentry/sentry-dotnet/pull/4122))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#084)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.8.3...0.8.4)

## 5.7.0

### Features

- New source generator allows Sentry to see true build variables like PublishAot and PublishTrimmed to properly adapt checks in the Sentry SDK ([#4101](https://github.com/getsentry/sentry-dotnet/pull/4101))
- Auto breadcrumbs now include all .NET MAUI gesture recognizer events ([#4124](https://github.com/getsentry/sentry-dotnet/pull/4124))
- Associate replays with errors and traces on Android ([#4133](https://github.com/getsentry/sentry-dotnet/pull/4133))

### Fixes

- Redact Authorization headers before sending events to Sentry ([#4164](https://github.com/getsentry/sentry-dotnet/pull/4164))
- Remove Strong Naming from Sentry.Hangfire ([#4099](https://github.com/getsentry/sentry-dotnet/pull/4099))
- Increase `RequestSize.Small` threshold from 1 kB to 4 kB to match other SDKs ([#4177](https://github.com/getsentry/sentry-dotnet/pull/4177))

### Dependencies

- Bump CLI from v2.43.1 to v2.45.0 ([#4169](https://github.com/getsentry/sentry-dotnet/pull/4169), [#4179](https://github.com/getsentry/sentry-dotnet/pull/4179))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2450)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.43.1...2.45.0)

## 5.7.0-beta.0

### Features

- When setting a transaction on the scope, the SDK will attempt to sync the transaction's trace context with the SDK on the native layer. Finishing a transaction will now also start a new trace ([#4153](https://github.com/getsentry/sentry-dotnet/pull/4153))
- Added `CaptureFeedback` overload with `configureScope` parameter ([#4073](https://github.com/getsentry/sentry-dotnet/pull/4073))
- Custom SessionReplay masks in MAUI Android apps ([#4121](https://github.com/getsentry/sentry-dotnet/pull/4121))

### Fixes

- Work around iOS SHA1 bug ([#4143](https://github.com/getsentry/sentry-dotnet/pull/4143))
- Prevent Auto Breadcrumbs Event Binder from leaking and rebinding events ([#4159](https://github.com/getsentry/sentry-dotnet/pull/4159))
- Fixes build error when building .NET Framework applications using Sentry 5.6.0: `MSB4185 :The function "IsWindows" on type "System.OperatingSystem" is not available` ([#4160](https://github.com/getsentry/sentry-dotnet/pull/4160))
- Added a `SentrySetCommitReleaseOptions` build property that can be specified separately from `SentryReleaseOptions` ([#4109](https://github.com/getsentry/sentry-dotnet/pull/4109))

### Dependencies

- Bump CLI from v2.43.0 to v2.43.1 ([#4151](https://github.com/getsentry/sentry-dotnet/pull/4151))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2431)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.43.0...2.43.1)

## 5.6.0

### Features

- Option to disable the SentryNative integration ([#4107](https://github.com/getsentry/sentry-dotnet/pull/4107), [#4134](https://github.com/getsentry/sentry-dotnet/pull/4134))
  - To disable it, add this msbuild property: `<SentryNative>false</SentryNative>`
- Reintroduced experimental support for Session Replay on Android ([#4097](https://github.com/getsentry/sentry-dotnet/pull/4097))
- If an incoming HTTP request has the `traceparent` header, it is now parsed and interpreted like the `sentry-trace` header. Outgoing requests now contain the `traceparent` header to facilitate integration with servesr that only support the [W3C Trace Context](https://www.w3.org/TR/trace-context/). ([#4084](https://github.com/getsentry/sentry-dotnet/pull/4084))

### Fixes

- Ensure user exception data is not removed by AspNetCoreExceptionProcessor ([#4016](https://github.com/getsentry/sentry-dotnet/pull/4106))
- Prevent users from disabling AndroidEnableAssemblyCompression which leads to untrappable crash ([#4089](https://github.com/getsentry/sentry-dotnet/pull/4089))
- Fixed MSVCRT build warning on Windows ([#4111](https://github.com/getsentry/sentry-dotnet/pull/4111))

### Dependencies

- Bump Cocoa SDK from v8.39.0 to v8.46.0 ([#4103](https://github.com/getsentry/sentry-dotnet/pull/4103))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8460)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.39.0...8.46.0)
- Bump Native SDK from v0.8.3 to v0.8.4 ([#4122](https://github.com/getsentry/sentry-dotnet/pull/4122))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#084)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.8.3...0.8.4)

## 5.7.0

### Features

- New source generator allows Sentry to see true build variables like PublishAot and PublishTrimmed to properly adapt checks in the Sentry SDK ([#4101](https://github.com/getsentry/sentry-dotnet/pull/4101))
- Auto breadcrumbs now include all .NET MAUI gesture recognizer events ([#4124](https://github.com/getsentry/sentry-dotnet/pull/4124))
- Associate replays with errors and traces on Android ([#4133](https://github.com/getsentry/sentry-dotnet/pull/4133))

### Fixes

- Redact Authorization headers before sending events to Sentry ([#4164](https://github.com/getsentry/sentry-dotnet/pull/4164))
- Remove Strong Naming from Sentry.Hangfire ([#4099](https://github.com/getsentry/sentry-dotnet/pull/4099))
- Increase `RequestSize.Small` threshold from 1 kB to 4 kB to match other SDKs ([#4177](https://github.com/getsentry/sentry-dotnet/pull/4177))

### Dependencies

- Bump CLI from v2.43.1 to v2.45.0 ([#4169](https://github.com/getsentry/sentry-dotnet/pull/4169), [#4179](https://github.com/getsentry/sentry-dotnet/pull/4179))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2450)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.43.1...2.45.0)

## 5.7.0-beta.0

### Features

- When setting a transaction on the scope, the SDK will attempt to sync the transaction's trace context with the SDK on the native layer. Finishing a transaction will now also start a new trace ([#4153](https://github.com/getsentry/sentry-dotnet/pull/4153))
- Added `CaptureFeedback` overload with `configureScope` parameter ([#4073](https://github.com/getsentry/sentry-dotnet/pull/4073))
- Custom SessionReplay masks in MAUI Android apps ([#4121](https://github.com/getsentry/sentry-dotnet/pull/4121))

### Fixes

- Work around iOS SHA1 bug ([#4143](https://github.com/getsentry/sentry-dotnet/pull/4143))
- Prevent Auto Breadcrumbs Event Binder from leaking and rebinding events ([#4159](https://github.com/getsentry/sentry-dotnet/pull/4159))
- Fixes build error when building .NET Framework applications using Sentry 5.6.0: `MSB4185 :The function "IsWindows" on type "System.OperatingSystem" is not available` ([#4160](https://github.com/getsentry/sentry-dotnet/pull/4160))
- Added a `SentrySetCommitReleaseOptions` build property that can be specified separately from `SentryReleaseOptions` ([#4109](https://github.com/getsentry/sentry-dotnet/pull/4109))

### Dependencies

- Bump CLI from v2.43.0 to v2.43.1 ([#4151](https://github.com/getsentry/sentry-dotnet/pull/4151))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2431)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.43.0...2.43.1)

## 5.6.0

### Features

- Option to disable the SentryNative integration ([#4107](https://github.com/getsentry/sentry-dotnet/pull/4107), [#4134](https://github.com/getsentry/sentry-dotnet/pull/4134))
  - To disable it, add this msbuild property: `<SentryNative>false</SentryNative>`
- Reintroduced experimental support for Session Replay on Android ([#4097](https://github.com/getsentry/sentry-dotnet/pull/4097))
- If an incoming HTTP request has the `traceparent` header, it is now parsed and interpreted like the `sentry-trace` header. Outgoing requests now contain the `traceparent` header to facilitate integration with servesr that only support the [W3C Trace Context](https://www.w3.org/TR/trace-context/). ([#4084](https://github.com/getsentry/sentry-dotnet/pull/4084))

### Fixes

- Ensure user exception data is not removed by AspNetCoreExceptionProcessor ([#4016](https://github.com/getsentry/sentry-dotnet/pull/4106))
- Prevent users from disabling AndroidEnableAssemblyCompression which leads to untrappable crash ([#4089](https://github.com/getsentry/sentry-dotnet/pull/4089))
- Fixed MSVCRT build warning on Windows ([#4111](https://github.com/getsentry/sentry-dotnet/pull/4111))

### Dependencies

- Bump Cocoa SDK from v8.39.0 to v8.46.0 ([#4103](https://github.com/getsentry/sentry-dotnet/pull/4103))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8460)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.39.0...8.46.0)
- Bump Native SDK from v0.8.3 to v0.8.4 ([#4122](https://github.com/getsentry/sentry-dotnet/pull/4122))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#084)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.8.3...0.8.4)

## 5.7.0

### Features

- New source generator allows Sentry to see true build variables like PublishAot and PublishTrimmed to properly adapt checks in the Sentry SDK ([#4101](https://github.com/getsentry/sentry-dotnet/pull/4101))
- Auto breadcrumbs now include all .NET MAUI gesture recognizer events ([#4124](https://github.com/getsentry/sentry-dotnet/pull/4124))
- Associate replays with errors and traces on Android ([#4133](https://github.com/getsentry/sentry-dotnet/pull/4133))

### Fixes

- Redact Authorization headers before sending events to Sentry ([#4164](https://github.com/getsentry/sentry-dotnet/pull/4164))
- Remove Strong Naming from Sentry.Hangfire ([#4099](https://github.com/getsentry/sentry-dotnet/pull/4099))
- Increase `RequestSize.Small` threshold from 1 kB to 4 kB to match other SDKs ([#4177](https://github.com/getsentry/sentry-dotnet/pull/4177))

### Dependencies

- Bump CLI from v2.43.1 to v2.45.0 ([#4169](https://github.com/getsentry/sentry-dotnet/pull/4169), [#4179](https://github.com/getsentry/sentry-dotnet/pull/4179))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2450)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.43.1...2.45.0)

## 5.7.0-beta.0

### Features

- When setting a transaction on the scope, the SDK will attempt to sync the transaction's trace context with the SDK on the native layer. Finishing a transaction will now also start a new trace ([#4153](https://github.com/getsentry/sentry-dotnet/pull/4153))
- Added `CaptureFeedback` overload with `configureScope` parameter ([#4073](https://github.com/getsentry/sentry-dotnet/pull/4073))
- Custom SessionReplay masks in MAUI Android apps ([#4121](https://github.com/getsentry/sentry-dotnet/pull/4121))

### Fixes

- Work around iOS SHA1 bug ([#4143](https://github.com/getsentry/sentry-dotnet/pull/4143))
- Prevent Auto Breadcrumbs Event Binder from leaking and rebinding events ([#4159](https://github.com/getsentry/sentry-dotnet/pull/4159))
- Fixes build error when building .NET Framework applications using Sentry 5.6.0: `MSB4185 :The function "IsWindows" on type "System.OperatingSystem" is not available` ([#4160](https://github.com/getsentry/sentry-dotnet/pull/4160))
- Added a `SentrySetCommitReleaseOptions` build property that can be specified separately from `SentryReleaseOptions` ([#4109](https://github.com/getsentry/sentry-dotnet/pull/4109))

### Dependencies

- Bump CLI from v2.43.0 to v2.43.1 ([#4151](https://github.com/getsentry/sentry-dotnet/pull/4151))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2431)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.43.0...2.43.1)

## 5.6.0

### Features

- Option to disable the SentryNative integration ([#4107](https://github.com/getsentry/sentry-dotnet/pull/4107), [#4134](https://github.com/getsentry/sentry-dotnet/pull/4134))
  - To disable it, add this msbuild property: `<SentryNative>false</SentryNative>`
- Reintroduced experimental support for Session Replay on Android ([#4097](https://github.com/getsentry/sentry-dotnet/pull/4097))
- If an incoming HTTP request has the `traceparent` header, it is now parsed and interpreted like the `sentry-trace` header. Outgoing requests now contain the `traceparent` header to facilitate integration with servesr that only support the [W3C Trace Context](https://www.w3.org/TR/trace-context/). ([#4084](https://github.com/getsentry/sentry-dotnet/pull/4084))

### Fixes

- Ensure user exception data is not removed by AspNetCoreExceptionProcessor ([#4016](https://github.com/getsentry/sentry-dotnet/pull/4106))
- Prevent users from disabling AndroidEnableAssemblyCompression which leads to untrappable crash ([#4089](https://github.com/getsentry/sentry-dotnet/pull/4089))
- Fixed MSVCRT build warning on Windows ([#4111](https://github.com/getsentry/sentry-dotnet/pull/4111))

### Dependencies

- Bump Cocoa SDK from v8.39.0 to v8.46.0 ([#4103](https://github.com/getsentry/sentry-dotnet/pull/4103))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8460)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.39.0...8.46.0)
- Bump Native SDK from v0.8.3 to v0.8.4 ([#4122](https://github.com/getsentry/sentry-dotnet/pull/4122))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#084)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.8.3...0.8.4)

## 5.7.0

### Features

- New source generator allows Sentry to see true build variables like PublishAot and PublishTrimmed to properly adapt checks in the Sentry SDK ([#4101](https://github.com/getsentry/sentry-dotnet/pull/4101))
- Auto breadcrumbs now include all .NET MAUI gesture recognizer events ([#4124](https://github.com/getsentry/sentry-dotnet/pull/4124))
- Associate replays with errors and traces on Android ([#4133](https://github.com/getsentry/sentry-dotnet/pull/4133))

### Fixes

- Redact Authorization headers before sending events to Sentry ([#4164](https://github.com/getsentry/sentry-dotnet/pull/4164))
- Remove Strong Naming from Sentry.Hangfire ([#4099](https://github.com/getsentry/sentry-dotnet/pull/4099))
- Increase `RequestSize.Small` threshold from 1 kB to 4 kB to match other SDKs ([#4177](https://github.com/getsentry/sentry-dotnet/pull/4177))

### Dependencies

- Bump CLI from v2.43.1 to v2.45.0 ([#4169](https://github.com/getsentry/sentry-dotnet/pull/4169), [#4179](https://github.com/getsentry/sentry-dotnet/pull/4179))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2450)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.43.1...2.45.0)

## 5.7.0-beta.0

### Features

- When setting a transaction on the scope, the SDK will attempt to sync the transaction's trace context with the SDK on the native layer. Finishing a transaction will now also start a new trace ([#4153](https://github.com/getsentry/sentry-dotnet/pull/4153))
- Added `CaptureFeedback` overload with `configureScope` parameter ([#4073](https://github.com/getsentry/sentry-dotnet/pull/4073))
- Custom SessionReplay masks in MAUI Android apps ([#4121](https://github.com/getsentry/sentry-dotnet/pull/4121))

### Fixes

- Work around iOS SHA1 bug ([#4143](https://github.com/getsentry/sentry-dotnet/pull/4143))
- Prevent Auto Breadcrumbs Event Binder from leaking and rebinding events ([#4159](https://github.com/getsentry/sentry-dotnet/pull/4159))
- Fixes build error when building .NET Framework applications using Sentry 5.6.0: `MSB4185 :The function "IsWindows" on type "System.OperatingSystem" is not available` ([#4160](https://github.com/getsentry/sentry-dotnet/pull/4160))
- Added a `SentrySetCommitReleaseOptions` build property that can be specified separately from `SentryReleaseOptions` ([#4109](https://github.com/getsentry/sentry-dotnet/pull/4109))

### Dependencies

- Bump CLI from v2.43.0 to v2.43.1 ([#4151](https://github.com/getsentry/sentry-dotnet/pull/4151))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2431)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.43.0...2.43.1)

## 5.6.0

### Features

- Option to disable the SentryNative integration ([#4107](https://github.com/getsentry/sentry-dotnet/pull/4107), [#4134](https://github.com/getsentry/sentry-dotnet/pull/4134))
  - To disable it, add this msbuild property: `<SentryNative>false</SentryNative>`
- Reintroduced experimental support for Session Replay on Android ([#4097](https://github.com/getsentry/sentry-dotnet/pull/4097))
- If an incoming HTTP request has the `traceparent` header, it is now parsed and interpreted like the `sentry-trace` header. Outgoing requests now contain the `traceparent` header to facilitate integration with servesr that only support the [W3C Trace Context](https://www.w3.org/TR/trace-context/). ([#4084](https://github.com/getsentry/sentry-dotnet/pull/4084))

### Fixes

- Ensure user exception data is not removed by AspNetCoreExceptionProcessor ([#4016](https://github.com/getsentry/sentry-dotnet/pull/4106))
- Prevent users from disabling AndroidEnableAssemblyCompression which leads to untrappable crash ([#4089](https://github.com/getsentry/sentry-dotnet/pull/4089))
- Fixed MSVCRT build warning on Windows ([#4111](https://github.com/getsentry/sentry-dotnet/pull/4111))

### Dependencies

- Bump Cocoa SDK from v8.39.0 to v8.46.0 ([#4103](https://github.com/getsentry/sentry-dotnet/pull/4103))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8460)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.39.0...8.46.0)
- Bump Native SDK from v0.8.3 to v0.8.4 ([#4122](https://github.com/getsentry/sentry-dotnet/pull/4122))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#084)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.8.3...0.8.4)

