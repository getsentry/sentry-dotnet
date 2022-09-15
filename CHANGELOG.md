# Changelog

## Unreleased

## Features

- `SentryOptions.AttachStackTrace` is now enabled by default. ([#1907](https://github.com/getsentry/sentry-dotnet/pull/1907))
- Update Sentry Android SDK to version 6.4.1 ([#1911](https://github.com/getsentry/sentry-dotnet/pull/1911))
- Update Sentry Cocoa SDK to version 7.24.1 ([#1912](https://github.com/getsentry/sentry-dotnet/pull/1912))
- Add TransactionNameSource annotation ([#1910](https://github.com/getsentry/sentry-dotnet/pull/1910))
- Use URL path in transaction names instead of "Unknown Route" ([#1919](https://github.com/getsentry/sentry-dotnet/pull/1919))

## Fixes

- Reduce lock contention when sampling ([#1915](https://github.com/getsentry/sentry-dotnet/pull/1915))

## 3.21.0

_Includes Sentry.Maui Preview 3_

## Features

- Add ISentryTransactionProcessor ([#1862](https://github.com/getsentry/sentry-dotnet/pull/1862))
- Added 'integrations' to SdkVersion ([#1820](https://github.com/getsentry/sentry-dotnet/pull/1820))
- Updated Sentry Android SDK to version 6.3.0 ([#1826](https://github.com/getsentry/sentry-dotnet/pull/1826))
- Add the Sentry iOS SDK ([#1829](https://github.com/getsentry/sentry-dotnet/pull/1829))
- Enable Scope Sync for iOS ([#1834](https://github.com/getsentry/sentry-dotnet/pull/1834))
- Add API for deliberately crashing an app ([#1842](https://github.com/getsentry/sentry-dotnet/pull/1842))
- Add Mac Catalyst target ([#1848](https://github.com/getsentry/sentry-dotnet/pull/1848))
- Add `Distribution` properties ([#1851](https://github.com/getsentry/sentry-dotnet/pull/1851))
- Add and configure options for the iOS SDK ([#1849](https://github.com/getsentry/sentry-dotnet/pull/1849))
- Set default `Release` and `Distribution` for iOS and Android ([#1856](https://github.com/getsentry/sentry-dotnet/pull/1856))
- Apply WinUI 3 exception handler in Sentry core ([#1863](https://github.com/getsentry/sentry-dotnet/pull/1863))
- Copy context info from iOS ([#1884](https://github.com/getsentry/sentry-dotnet/pull/1884))

### Fixes

- Parse "Mono Unity IL2CPP" correctly in platform runtime name ([#1742](https://github.com/getsentry/sentry-dotnet/pull/1742))
- Fix logging loop with NLog sentry ([#1824](https://github.com/getsentry/sentry-dotnet/pull/1824))
- Fix logging loop with Serilog sentry ([#1828](https://github.com/getsentry/sentry-dotnet/pull/1828))
- Skip attachment if stream is empty ([#1854](https://github.com/getsentry/sentry-dotnet/pull/1854))
- Allow some mobile options to be modified from defaults ([#1857](https://github.com/getsentry/sentry-dotnet/pull/1857))
- Fix environment name casing issue ([#1861](https://github.com/getsentry/sentry-dotnet/pull/1861))
- Null check HttpContext in SystemWebVersionLocator ([#1881](https://github.com/getsentry/sentry-dotnet/pull/1881))
- Fix detection of .NET Framework 4.8.1 ([#1885](https://github.com/getsentry/sentry-dotnet/pull/1885))
- Flush caching transport with main flush ([#1890](https://github.com/getsentry/sentry-dotnet/pull/1890))
- Fix Sentry interfering with MAUI's focus events ([#1891](https://github.com/getsentry/sentry-dotnet/pull/1891))
- Stop using `server-os` and `server-runtime` ([#1893](https://github.com/getsentry/sentry-dotnet/pull/1893))

## 3.20.1

### Fixes

- URGENT: Fix events rejected due to duplicate `sent_at` header when offline caching is enabled through `CacheDirectoryPath` ([#1818](https://github.com/getsentry/sentry-dotnet/pull/1818))
- Fix null ref in aspnet TryGetTraceHeader ([#1807](https://github.com/getsentry/sentry-dotnet/pull/1807))

## 3.20.0

### Features

- Use `sent_at` instead of `sentry_timestamp` to reduce clock skew ([#1690](https://github.com/getsentry/sentry-dotnet/pull/1690)) 
- Send project root path with events ([#1739](https://github.com/getsentry/sentry-dotnet/pull/1739))

### Fixes

- Detect MVC versioning in route ([#1731](https://github.com/getsentry/sentry-dotnet/pull/1731))
- Fix error with `ConcurrentHashMap` on Android <= 9 ([#1761](https://github.com/getsentry/sentry-dotnet/pull/1761))
- Minor improvements to `BackgroundWorker` ([#1773](https://github.com/getsentry/sentry-dotnet/pull/1773))
- Make GzipRequestBodyHandler respect async ([#1776](https://github.com/getsentry/sentry-dotnet/pull/1776))
- Fix race condition in handling of `InitCacheFlushTimeout` ([#1784](https://github.com/getsentry/sentry-dotnet/pull/1784))
- Fix exceptions on background thread not reported in Unity ([#1794](https://github.com/getsentry/sentry-dotnet/pull/1794))

## 3.19.0

_Includes Sentry.Maui Preview 2_

### Features

- Expose `EnumerateChainedExceptions` ([#1733](https://github.com/getsentry/sentry-dotnet/pull/1733))
- Android Scope Sync ([#1737](https://github.com/getsentry/sentry-dotnet/pull/1737))
- Enable logging in MAUI ([#1738](https://github.com/getsentry/sentry-dotnet/pull/1738))
- Support `IntPtr` and `UIntPtr` serialization ([#1746](https://github.com/getsentry/sentry-dotnet/pull/1746))
- Log Warning when secret is detected in DSN ([#1749](https://github.com/getsentry/sentry-dotnet/pull/1749))
- Catch permission exceptions on Android ([#1750](https://github.com/getsentry/sentry-dotnet/pull/1750))
- Enable offline caching in MAUI ([#1753](https://github.com/getsentry/sentry-dotnet/pull/1753))
- Send client report when flushing queue ([#1757](https://github.com/getsentry/sentry-dotnet/pull/1757))

### Fixes

- Set MAUI minimum version ([#1728](https://github.com/getsentry/sentry-dotnet/pull/1728))
- Don't allow `SentryDiagnosticListenerIntegration` to be added multiple times ([#1748](https://github.com/getsentry/sentry-dotnet/pull/1748))
- Catch permission exceptions for MAUI ([#1750](https://github.com/getsentry/sentry-dotnet/pull/1750))
- Don't allow newlines in diagnostic logger messages ([#1756](https://github.com/getsentry/sentry-dotnet/pull/1756))

## 3.18.0

_Includes Sentry.Maui Preview 1_

### Features

- Move tunnel functionality into Sentry.AspNetCore ([#1645](https://github.com/getsentry/sentry-dotnet/pull/1645))
- Make `HttpContext` available for sampling decisions ([#1682](https://github.com/getsentry/sentry-dotnet/pull/1682))
- Send the .NET Runtime Identifier to Sentry ([#1708](https://github.com/getsentry/sentry-dotnet/pull/1708))
- Added a new `net6.0-android` target for the `Sentry` core library, which bundles the [Sentry Android SDK](https://docs.sentry.io/platforms/android/):
  - Initial .NET 6 Android support ([#1288](https://github.com/getsentry/sentry-dotnet/pull/1288))
  - Update Android Support ([#1669](https://github.com/getsentry/sentry-dotnet/pull/1669))
  - Update Sentry-Android to 6.0.0-rc.1 ([#1686](https://github.com/getsentry/sentry-dotnet/pull/1686))
  - Update Sentry-Android to 6.0.0 ([#1697](https://github.com/getsentry/sentry-dotnet/pull/1697))
  - Set Java/Android SDK options ([#1694](https://github.com/getsentry/sentry-dotnet/pull/1694))
  - Refactor and update Android options ([#1705](https://github.com/getsentry/sentry-dotnet/pull/1705))
  - Add Android OS information to the event context ([#1716](https://github.com/getsentry/sentry-dotnet/pull/1716))
- Added a new `Sentry.Maui` integration library for the [.NET MAUI](https://dotnet.microsoft.com/apps/maui) platform:
  - Initial MAUI support ([#1663](https://github.com/getsentry/sentry-dotnet/pull/1663))
  - Continue with adding MAUI support ([#1670](https://github.com/getsentry/sentry-dotnet/pull/1670))
  - MAUI events become extra context in Sentry events ([#1706](https://github.com/getsentry/sentry-dotnet/pull/1706))
  - Add options for PII breadcrumbs from MAUI events ([#1709](https://github.com/getsentry/sentry-dotnet/pull/1709))
  - Add device information to the event context ([#1713](https://github.com/getsentry/sentry-dotnet/pull/1713))
  - Add platform OS information to the event context ([#1717](https://github.com/getsentry/sentry-dotnet/pull/1717))

### Fixes

- Remove IInternalSdkIntegration ([#1656](https://github.com/getsentry/sentry-dotnet/pull/1656))
- On async Main, dont unregister unhandled exception before capturing crash  ([#321](https://github.com/getsentry/sentry-dotnet/issues/321))
- Handle BadHttpRequestException from Kestrel inside SentryTunnelMiddleware ([#1673](https://github.com/getsentry/sentry-dotnet/pull/1673))
- Improve timestamp precision of transactions and spans ([#1680](https://github.com/getsentry/sentry-dotnet/pull/1680))
- Flatten AggregateException ([#1672](https://github.com/getsentry/sentry-dotnet/pull/1672))
  - NOTE: This can affect grouping. You can keep the original behavior by setting the option `KeepAggregateException` to `true`.
- Serialize stack frame addresses as strings. ([#1692](https://github.com/getsentry/sentry-dotnet/pull/1692))
- Improve serialization perf and fix memory leak in `SentryEvent` ([#1693](https://github.com/getsentry/sentry-dotnet/pull/1693))
- Add type checking in contexts TryGetValue ([#1700](https://github.com/getsentry/sentry-dotnet/pull/1700))
- Restore serialization of the `Platform` name ([#1702](https://github.com/getsentry/sentry-dotnet/pull/1702))

## 3.17.1

### Fixes

- Rework how the `InitCacheFlushTimeout` option is implemented. ([#1644](https://github.com/getsentry/sentry-dotnet/pull/1644))
- Add retry logic to the caching transport when moving files back from the processing folder. ([#1649](https://github.com/getsentry/sentry-dotnet/pull/1649))

## 3.17.0

**Notice:** If you are using self-hosted Sentry, this version and forward requires either Sentry version >= [21.9.0](https://github.com/getsentry/relay/blob/master/CHANGELOG.md#2190), or you must manually disable sending client reports via the `SendClientReports` option.

### Features

- Collect and send Client Reports to Sentry, which contain counts of discarded events. ([#1556](https://github.com/getsentry/sentry-dotnet/pull/1556))
- Expose `ITransport` and `SentryOptions.Transport` public, to support using custom transports ([#1602](https://github.com/getsentry/sentry-dotnet/pull/1602))
- Android native crash support ([#1288](https://github.com/getsentry/sentry-dotnet/pull/1288))

### Fixes

- Workaround `System.Text.Json` issue with Unity IL2CPP. ([#1583](https://github.com/getsentry/sentry-dotnet/pull/1583))
- Demystify stack traces for exceptions that fire in a `BeforeSend` callback. ([#1587](https://github.com/getsentry/sentry-dotnet/pull/1587))
- Obsolete `Platform` and always write `csharp` ([#1610](https://github.com/getsentry/sentry-dotnet/pull/1610))
- Fix a minor issue in the caching transport related to recovery of files from previous session. ([#1617](https://github.com/getsentry/sentry-dotnet/pull/1617))
- Better DisableAppDomainProcessExitFlush docs ([#1634](https://github.com/getsentry/sentry-dotnet/pull/1634))

## 3.16.0

### Features

- Use a default value of 60 seconds if a `Retry-After` header is not present. ([#1537](https://github.com/getsentry/sentry-dotnet/pull/1537))
- Add new Protocol definitions for DebugImages and AddressMode ([#1513](https://github.com/getsentry/sentry-dotnet/pull/1513))
- Add `HttpTransport` extensibility and synchronous serialization support ([#1560](https://github.com/getsentry/sentry-dotnet/pull/1560))
- Add `UseAsyncFileIO` to Sentry options (enabled by default) ([#1564](https://github.com/getsentry/sentry-dotnet/pull/1564))

### Fixes

- Fix event dropped by bad attachment when no logger is set. ([#1557](https://github.com/getsentry/sentry-dotnet/pull/1557))
- Ignore zero properties for MemoryInfo ([#1531](https://github.com/getsentry/sentry-dotnet/pull/1531))
- Cleanup diagnostic source ([#1529](https://github.com/getsentry/sentry-dotnet/pull/1529))
- Remove confusing message Successfully sent cached envelope ([#1542](https://github.com/getsentry/sentry-dotnet/pull/1542))
- Fix infinite loop in SentryDatabaseLogging.UseBreadcrumbs ([#1543](https://github.com/getsentry/sentry-dotnet/pull/1543))
- GetFromRuntimeInformation() in try-catch  ([#1554](https://github.com/getsentry/sentry-dotnet/pull/1554))
- Make `Contexts` properties more thread-safe ([#1571](https://github.com/getsentry/sentry-dotnet/pull/1571))
- Fix `PlatformNotSupportedException` exception on `net6.0-maccatalyst` targets ([#1567](https://github.com/getsentry/sentry-dotnet/pull/1567))
- In ASP.Net Core, make sure that `SentrySdk.LastEventId` is accessible from exception handler pages ([#1573](https://github.com/getsentry/sentry-dotnet/pull/1573))

## 3.15.0

### Features

- Expose ConfigureAppFrame as a public static function. ([#1493](https://github.com/getsentry/sentry-dotnet/pull/1493))

### Fixes

- Make `SentryDiagnosticSubscriber._disposableListeners` thread safe ([#1506](https://github.com/getsentry/sentry-dotnet/pull/1506))
- Adjust database span names by replacing `_` to `.`. `db.query_compiler` becomes `db.query.compile`. ([#1502](https://github.com/getsentry/sentry-dotnet/pull/1502))

## 3.14.1

### Fixes

- Fix caching transport with attachments ([#1489](https://github.com/getsentry/sentry-dotnet/pull/1489))
- Revert Sentry in implicit usings ([#1490](https://github.com/getsentry/sentry-dotnet/pull/1490))

## 3.14.0

### Features

- Add the delegate TransactionNameProvider to allow the name definition from Unknown transactions on ASP.NET Core ([#1421](https://github.com/getsentry/sentry-dotnet/pull/1421))
- SentrySDK.WithScope is now obsolete in favour of overloads of CaptureEvent, CaptureMessage, CaptureException ([#1412](https://github.com/getsentry/sentry-dotnet/pull/1412))
- Add Sentry to global usings when ImplicitUsings is enabled (`<ImplicitUsings>true</ImplicitUsings>`) ([#1398](https://github.com/getsentry/sentry-dotnet/pull/1398))
- The implementation of the background worker can now be changed ([#1450](https://github.com/getsentry/sentry-dotnet/pull/1450))
- Map reg key 528449 to net48 ([#1465](https://github.com/getsentry/sentry-dotnet/pull/1465))
- Improve logging for failed JSON serialization ([#1473](https://github.com/getsentry/sentry-dotnet/pull/1473))

### Fixes

- Handle exception from crashedLastRun callback ([#1328](https://github.com/getsentry/sentry-dotnet/pull/1328))
- Reduced the logger noise from EF when not using Performance Monitoring ([#1441](https://github.com/getsentry/sentry-dotnet/pull/1441))
- Create CachingTransport directories in constructor to avoid DirectoryNotFoundException ([#1432](https://github.com/getsentry/sentry-dotnet/pull/1432))
- UnobservedTaskException is now considered as Unhandled ([#1447](https://github.com/getsentry/sentry-dotnet/pull/1447))
- Avoid calls the Thread.CurrentThread where possible ([#1466](https://github.com/getsentry/sentry-dotnet/pull/1466))
- Rename thread pool protocol keys to snake case ([#1472](https://github.com/getsentry/sentry-dotnet/pull/1472))
- Treat IOException as a network issue ([#1476](https://github.com/getsentry/sentry-dotnet/pull/1476))
- Fix incorrect sdk name in envelope header ([#1474](https://github.com/getsentry/sentry-dotnet/pull/1474))
- Use Trace.WriteLine for TraceDiagnosticLogger ([#1475](https://github.com/getsentry/sentry-dotnet/pull/1475))
- Remove Exception filters to work around Unity bug on 2019.4.35f IL2CPP ([#1486](https://github.com/getsentry/sentry-dotnet/pull/1486))

## 3.13.0

### Features

- Add CaptureLastError as an extension method to the Server class on ASP.NET ([#1411](https://github.com/getsentry/sentry-dotnet/pull/1411))
- Add IsDynamicCode* to events ([#1418](https://github.com/getsentry/sentry-dotnet/pull/1418))

### Fixes

- Dispose of client should only flush ([#1354](https://github.com/getsentry/sentry-dotnet/pull/1354))

## 3.12.3

### Fixes

- Events no longer get dropped because of non-serializable contexts or attachments ([#1401](https://github.com/getsentry/sentry-dotnet/pull/1401))
- Add MemoryInfo to sentry event ([#1337](https://github.com/getsentry/sentry-dotnet/pull/1337))
- Report ThreadPool stats ([#1399](https://github.com/getsentry/sentry-dotnet/pull/1399))

## 3.12.2

### Fixes

- log through serialization ([#1388](https://github.com/getsentry/sentry-dotnet/pull/1388))
- Attaching byte arrays to the scope no longer leads to ObjectDisposedException ([#1384](https://github.com/getsentry/sentry-dotnet/pull/1384))
- Operation cancel while flushing cache no longer logs an errors ([#1352](https://github.com/getsentry/sentry-dotnet/pull/1352))
- Dont fail for attachment read error ([#1378](https://github.com/getsentry/sentry-dotnet/pull/1378))
- Fix file locking in attachments ([#1377](https://github.com/getsentry/sentry-dotnet/pull/1377))

## 3.12.1

### Features

- Dont log "Ignoring request with Size" when null ([#1348](https://github.com/getsentry/sentry-dotnet/pull/1348))
- Move to stable v6 for `Microsoft.Extensions.*` packages ([#1347](https://github.com/getsentry/sentry-dotnet/pull/1347))
- bump Ben.Demystifier adding support for Microsoft.Bcl.AsyncInterfaces([#1349](https://github.com/getsentry/sentry-dotnet/pull/1349))

### Fixes

- Fix EF Core garbage collected messages and ordering ([#1368](https://github.com/getsentry/sentry-dotnet/pull/1368))
- Update X-Sentry-Auth header to include correct sdk name and version ([#1333](https://github.com/getsentry/sentry-dotnet/pull/1333))

## 3.12.0

### Features

- Add automatic spans to Entity Framework operations ([#1107](https://github.com/getsentry/sentry-dotnet/pull/1107))

### Fixes

- Avoid using the same connection Span for the same ConnectionId ([#1317](https://github.com/getsentry/sentry-dotnet/pull/1317))
- Finish unfinished Spans on Transaction completion ([#1296](https://github.com/getsentry/sentry-dotnet/pull/1296))

## 3.12.0-alpha.1

### Features

- .NET 6 specific targets ([#939](https://github.com/getsentry/sentry-dotnet/pull/939))

## 3.11.1

### Fixes

- Forward the IP of the client with whe tunnel middleware ([#1310](getsentry/sentry-dotnet/pull/1310))

## 3.11.0

### Features

- Sentry Sessions status as Breadcrumbs ([#1263](https://github.com/getsentry/sentry-dotnet/pull/1263))
- Enhance GCP Integraction with performance monitoring and revision number ([#1286](https://github.com/getsentry/sentry-dotnet/pull/1286))
- Bump Ben.Demystifier to support .NET 6 ([#1290](https://github.com/getsentry/sentry-dotnet/pull/1290))

### Fixes

- ASP.NET Core: Data from Scope in options should be applied on each request ([#1270](https://github.com/getsentry/sentry-dotnet/pull/1270))
- Add missing `ConfigureAwaits(false)` for `async using` ([#1276](https://github.com/getsentry/sentry-dotnet/pull/1276))
- Fix missing handled tag when events are logged via an ASP.NET Core pipeline logger ([#1284](getsentry/sentry-dotnet/pull/1284))

## 3.10.0

### Features

- Add additional primitive values as tags on SentryLogger ([#1246](https://github.com/getsentry/sentry-dotnet/pull/1246))

### Fixes

- Events are now sent on Google Gloud Functions Integration ([#1249](https://github.com/getsentry/sentry-dotnet/pull/1249))
- Cache envelope headers ([#1242](https://github.com/getsentry/sentry-dotnet/pull/1242))
- Avoid replacing Transaction Name on ASP.NET Core by null or empty ([#1215](https://github.com/getsentry/sentry-dotnet/pull/1215))
- Ignore DiagnosticSource Integration if no Sampling available ([#1238](https://github.com/getsentry/sentry-dotnet/pull/1238))

## 3.9.4

### Fixes

- Unity Android support: check for native crashes before closing session as Abnormal ([#1222](https://github.com/getsentry/sentry-dotnet/pull/1222))

## 3.9.3

### Fixes

- Add missing PathBase from ASP.NET Core ([#1198](https://github.com/getsentry/sentry-dotnet/pull/1198))
- Use fallback if route pattern is MVC ([#1188](https://github.com/getsentry/sentry-dotnet/pull/1188))
- Move UseSentryTracing to different namespace ([#1200](https://github.com/getsentry/sentry-dotnet/pull/1200))
- Prevent duplicate package reporting ([#1197](https://github.com/getsentry/sentry-dotnet/pull/1197))

## 3.9.2

### Fixes

- Exceptions from UnhandledExceptionIntegration were not marking sessions as crashed ([#1193](https://github.com/getsentry/sentry-dotnet/pull/1193))

## 3.9.1

### Fixes

- Removed braces from tag keys on DefaultSentryScopeStateProcessor ([#1183](https://github.com/getsentry/sentry-dotnet/pull/1183))
- Fix SQLClient unplanned behaviors ([#1179](https://github.com/getsentry/sentry-dotnet/pull/1179))
- Add fallback to Scope Stack from AspNet ([#1180](https://github.com/getsentry/sentry-dotnet/pull/1180))

## 3.9.0

### Features

- EF Core and SQLClient performance monitoring integration ([#1154](https://github.com/getsentry/sentry-dotnet/pull/1154))
- Improved SDK diagnostic logs ([#1161](https://github.com/getsentry/sentry-dotnet/pull/1161))
- Add Scope observer to SentryOptions ([#1153](https://github.com/getsentry/sentry-dotnet/pull/1153))

### Fixes

- Fix end session from Hub adapter not being passed to SentrySDK ([#1158](https://github.com/getsentry/sentry-dotnet/pull/1158))
- Installation id catches dir not exist([#1159](https://github.com/getsentry/sentry-dotnet/pull/1159))
- Set error status to transaction if http has exception and ok status ([#1143](https://github.com/getsentry/sentry-dotnet/pull/1143))
- Fix max breadcrumbs limit when MaxBreadcrumbs is zero or lower ([#1145](https://github.com/getsentry/sentry-dotnet/pull/1145))

## 3.8.3

### Features

- New package Sentry.Tunnel to proxy Sentry events ([#1133](https://github.com/getsentry/sentry-dotnet/pull/1133))

### Fixes

- Avoid serializing dangerous types ([#1134](https://github.com/getsentry/sentry-dotnet/pull/1134))
- Don't cancel cache flushing on init ([#1139](https://github.com/getsentry/sentry-dotnet/pull/1139))

## 3.8.2

### Fixes

- Add IsParentSampled to ITransactionContext ([#1128](https://github.com/getsentry/sentry-dotnet/pull/1128)
- Avoid warn in global mode ([#1132](https://github.com/getsentry/sentry-dotnet/pull/1132))
- Fix `ParentSampledId` being reset on `Transaction` ([#1130](https://github.com/getsentry/sentry-dotnet/pull/1130))

## 3.8.1

### Fixes

- Persisted Sessions logging ([#1125](https://github.com/getsentry/sentry-dotnet/pull/1125))
- Don't log an error when attempting to recover a persisted session but none exists ([#1123](https://github.com/getsentry/sentry-dotnet/pull/1123))

### Features

- Introduce scope stack abstraction to support global scope on desktop and mobile applications and `HttpContext`-backed scoped on legacy ASP.NET ([#1124](https://github.com/getsentry/sentry-dotnet/pull/1124))

## 3.8.0

### Fixes

- ASP.NET Core: fix handled not being set for Handled exceptions ([#1111](https://github.com/getsentry/sentry-dotnet/pull/1111))

### Features

- File system persistence for sessions ([#1105](https://github.com/getsentry/sentry-dotnet/pull/1105))

## 3.7.0

### Features

- Add HTTP request breadcrumb ([#1113](https://github.com/getsentry/sentry-dotnet/pull/1113))
- Integration for Google Cloud Functions ([#1085](https://github.com/getsentry/sentry-dotnet/pull/1085))
- Add ClearAttachments to Scope ([#1104](https://github.com/getsentry/sentry-dotnet/pull/1104))
- Add additional logging and additional fallback for installation ID ([#1103](https://github.com/getsentry/sentry-dotnet/pull/1103))

### Fixes

- Avoid Unhandled Exception on .NET 461 if the Registry Access threw an exception ([#1101](https://github.com/getsentry/sentry-dotnet/pull/1101))

## 3.6.1

### Fixes

- `IHub.ResumeSession()`: don't start a new session if pause wasn't called or if there is no active session ([#1089](https://github.com/getsentry/sentry-dotnet/pull/1089))
- Fixed incorrect order when getting the last active span ([#1094](https://github.com/getsentry/sentry-dotnet/pull/1094))
- Fix logger call in BackgroundWorker that caused a formatting exception in runtime ([#1092](https://github.com/getsentry/sentry-dotnet/pull/1092))

## 3.6.0

### Features

- Implement pause & resume session ([#1069](https://github.com/getsentry/sentry-dotnet/pull/1069))
- Add auto session tracking ([#1068](https://github.com/getsentry/sentry-dotnet/pull/1068))
- Add SDK information to envelope ([#1084](https://github.com/getsentry/sentry-dotnet/pull/1084))
- Add ReportAssembliesMode in favor of ReportAssemblies ([#1079](https://github.com/getsentry/sentry-dotnet/pull/1079))

### Fixes

- System.Text.Json 5.0.2 ([#1078](https://github.com/getsentry/sentry-dotnet/pull/1078))

## 3.6.0-alpha.2

### Features

- Extended Device and GPU protocol; public IJsonSerializable ([#1063](https://github.com/getsentry/sentry-dotnet/pull/1063))
- ASP.NET Core: Option `AdjustStandardEnvironmentNameCasing` to opt-out from lower casing env name. [#1057](https://github.com/getsentry/sentry-dotnet/pull/1057)
- Sessions: Improve exception check in `CaptureEvent(...)` for the purpose of reporting errors in session ([#1058](https://github.com/getsentry/sentry-dotnet/pull/1058))
- Introduce TraceDiagnosticLogger and obsolete DebugDiagnosticLogger ([#1048](https://github.com/getsentry/sentry-dotnet/pull/1048))

### Fixes

- Handle error thrown while trying to get `BootTime` on PS4 with IL2CPP ([#1062](https://github.com/getsentry/sentry-dotnet/pull/1062))
- Use SentryId for ISession.Id ([#1052](https://github.com/getsentry/sentry-dotnet/pull/1052))
- Add System.Reflection.Metadata as a dependency for netcoreapp3.0 target([#1064](https://github.com/getsentry/sentry-dotnet/pull/1064))

## 3.6.0-alpha.1

### Features

- Implemented client-mode release health ([#1013](https://github.com/getsentry/sentry-dotnet/pull/1013))

### Fixes

- Report lowercase staging environment for ASP.NET Core ([#1046](https://github.com/getsentry/sentry-unity/pull/1046))

## 3.5.0

### Features

- Report user IP address for ASP.NET Core ([#1045](https://github.com/getsentry/sentry-unity/pull/1045))

### Fixes

- Connect middleware exceptions to transactions ([#1043](https://github.com/getsentry/sentry-dotnet/pull/1043))
- Hub.IsEnabled set to false when Hub disposed ([#1021](https://github.com/getsentry/sentry-dotnet/pull/1021))

## 3.4.0

### Features

- Sentry.EntityFramework moved to this repository ([#1017](https://github.com/getsentry/sentry-dotnet/pull/1017))
- Additional `netstandard2.1` target added. Sample with .NET Core 3.1 console app.
- `UseBreadcrumbs` is called automatically by `AddEntityFramework`

### Fixes

- Normalize line breaks ([#1016](https://github.com/getsentry/sentry-dotnet/pull/1016))
- Finish span with exception in SentryHttpMessageHandler ([#1037](https://github.com/getsentry/sentry-dotnet/pull/1037))

## 3.4.0-beta.0

### Features

- Serilog: Add support for Serilog.Formatting.ITextFormatter ([#998](https://github.com/getsentry/sentry-dotnet/pull/998))
- simplify ifdef ([#1010](https://github.com/getsentry/sentry-dotnet/pull/1010))
- Use `DebugDiagnosticLogger` as the default logger for legacy ASP.NET ([#1012](https://github.com/getsentry/sentry-dotnet/pull/1012))
- Adjust parameter type in `AddBreadcrumb` to use `IReadOnlyDictionary<...>` instead of `Dictionary<...>` ([#1000](https://github.com/getsentry/sentry-dotnet/pull/1000))
- await dispose everywhere ([#1009](https://github.com/getsentry/sentry-dotnet/pull/1009))
- Further simplify transaction integration from legacy ASP.NET ([#1011](https://github.com/getsentry/sentry-dotnet/pull/1011))

## 3.3.5-beta.0

### Features

- Default environment to "debug" if running with debugger attached (#978)
- ASP.NET Classic: `HttpContext.StartSentryTransaction()` extension method (#996)

### Fixes

- Unity can have negative line numbers ([#994](https://github.com/getsentry/sentry-dotnet/pull/994))
- Fixed an issue where an attempt to deserialize `Device` with a non-system time zone failed ([#993](https://github.com/getsentry/sentry-dotnet/pull/993))

## 3.3.4

### Features

- Env var to keep large envelopes if they are rejected by Sentry (#957)

### Fixes

- serialize parent_span_id in contexts.trace (#958)

## 3.3.3

### Fixes

- boot time detection can fail in some cases (#955)

## 3.3.2

### Fixes

- Don't override Span/Transaction status on Finish(...) if status was not provided explicitly (#928) @Tyrrrz
- Fix startup time shows incorrect value on macOS/Linux. Opt-out available for IL2CPP. (#948)

## 3.3.1

### Fixes

- Move Description field from Transaction to Trace context (#924) @Tyrrrz
- Drop unfinished spans from transaction (#923) @Tyrrrz
- Don't dispose the SDK when UnobservedTaskException is captured (#925) @bruno-garcia
- Fix spans not inheriting TraceId from transaction (#922) @Tyrrrz

## 3.3.0

### Features

- Add StartupTime and Device.BootTime (#887) @lucas-zimerman
- Link events to currently active span (#909) @Tyrrrz
- Add useful contextual data to TransactionSamplingContext in ASP.NET Core integration (#910) @Tyrrrz

### Changes

- Limit max spans in transaction to 1000 (#908) @Tyrrrz

## 3.2.0

### Changes

- Changed the underlying implementation of `ITransaction` and `ISpan`. `IHub.CaptureTransaction` now takes a `Transaction` instead of `ITransaction`. (#880) @Tyrrrz
- Add IsParentSampled to TransactionContext (#885) @Tyrrrz
- Retrieve CurrentVersion for ASP.NET applications (#884) @lucas-zimerman
- Make description parameter nullable on `ISpan.StartChild(...)` and related methods (#900) @Tyrrrz
- Add Platform to Transaction, mimicking the same property on SentryEvent (#901) @Tyrrrz

## 3.1.0

### Features

- Adding TaskUnobservedTaskExceptionIntegration to default integrations and method to remove it (#870) @FilipNemec
- Enrich transactions with more data (#875) @Tyrrrz

### Fixes
- Don't add version prefix in release if it's already set (#877) @Tyrrrz

## 3.0.8

### Features

- Add AddSentryTag and AddSentryContext Extensions for exception class (#834) @lucas-zimerman
- Associate span exceptions with event exceptions (#848) @Tyrrrz
- MaxCacheItems option to control files on disk (#846) @Tyrrrz
- Move SentryHttpMessageHandlerBuilderFilter to Sentry.Extensions.Logging (#845) @Tyrrrz

### Fixes

- Fix CachingTransport throwing an exception when it can't move the files from the previous session (#871) @Tyrrrz

## 3.0.7

### Changes
- Don't write timezone_display_name if it's the same as the ID (#837) @Tyrrrz
- Serialize arbitrary objects in contexts (#838) @Tyrrrz

## 3.0.6

### Fixes

- Fix serialization of transactions when filesystem caching is enabled. (#815) @Tyrrrz
- Fix UWP not registering exceptions (#821) @lucas-zimerman
- Fix tracing middleware (#813) @Tyrrrz

## 3.0.5

### Changes

- Fix transaction sampling (#810) @Tyrrrz

## 3.0.4

### Changes

- Don't add logs coming from Sentry as breadcrumbs (fixes stack overflow exception) (#797) @Tyrrrz
- Consolidate logic for resolving hub (fixes bug "SENTRY_DSN is not defined") (#795) @Tyrrrz
- Add SetFingerprint overload that takes `params string[]` (#796) @Tyrrrz
- Create spans for outgoing HTTP requests (#802) @Tyrrrz
- Finish span on exception in SentryHttpMessageHandler (#806) @Tyrrrz
- Fix ObjectDisposedException caused by object reuse in RetryAfterHandler (#807) @Tyrrrz

## 3.0.3

### Changes

- Fix DI issues in ASP.NET Core + SentryHttpMessageHandlerBuilderFilter (#789) @Tyrrrz
- Fix incorrect NRT on SpanContext.ctor (#788) @Tyrrrz
- Remove the `Evaluate` error from the breadcrumb list (#790) @Tyrrrz
- Set default tracing sample rate to 0.0 (#791) @Tyrrrz

## 3.0.2

### Changes

- Add GetSpan() to IHub and SentrySdk (#782) @Tyrrrz
- Automatically start transactions from incoming trace in ASP.NET Core (#783) @Tyrrrz
- Automatically inject 'sentry-trace' on outgoing requests in ASP.NET Core (#784) @Tyrrrz

## 3.0.1

### Changes

- bump log4net 2.0.12 (#781) @bruno-garcia
- Fix Serilog version (#780) @bruno-garcia
- Move main Protocol types to Sentry namespace (#779) @bruno-garcia

## 3.0.0

### Changes

- Add support for dynamic transaction sampling. (#753) @Tyrrrz
- Integrate trace headers. (#758) @Tyrrrz
- Renamed Option `DiagnosticsLevel` to `DiagnosticLevel` (#759) @bruno-garcia
- Add additional data to transactions (#763) @Tyrrrz
- Improve transaction instrumentation on ASP.NET Core (#766) @Tyrrrz
- Add `Release` to `Scope` (#765) @Tyrrrz
- Don't fallback to `HttpContext.RequestPath` if a route is unknown (#767 #769) @kanadaj @Tyrrrz

## 3.0.0-beta.0

### Changes

- Add instruction_addr to SentryStackFrame. (#744) @lucas-zimerman
- Default stack trace format: Ben.Demystifier (#732) @bruno-garcia

## 3.0.0-alpha.11

### Changed

- Limit attachment size (#705)
- Separate tracing middleware (#737)
- Bring Transaction a bit more inline with Java SDK (#741)
- Sync transaction and transaction name on scope (#740)

## 3.0.0-alpha.10

- Disabled Mono StackTrace Factory. (#709) @lucas-zimerman
- Adds to the existing User Other dict rather than replacing (#729) @brettjenkins

## 3.0.0-alpha.9

- Handle non-json error response messages on HttpTransport. (#690) @lucas-zimerman
- Fix deadlock on missing ConfigureAwait into foreach loops. (#694) @lucas-zimerman
- Report gRPC sdk name (#700) @bruno-garcia

## 3.0.0-alpha.8

- Include parameters in stack frames. (#662) @Tyrrrz
- Remove CultureUIInfo if value is even with CultureInfo. (#671) @lucas-zimerman
- Make all fields on UserFeedback optional. (#660) @Tyrrrz
- Align transaction names with Java. (#659) @Tyrrrz
- Include assembly name in default release. (#682) @Tyrrrz
- Add support for attachments. (#670) @Tyrrrz
- Improve logging for relay errors. (#683) @Tyrrrz
- Report sentry.dotnet.aspnet on the new Sentry.AspNet package. (#681) @Tyrrrz
- Always send a default release. (#695) @Tyrrrz

## 3.0.0-alpha.7

* Ref moved SentryId from namespace Sentry.Protocol to Sentry (#643) @lucas-zimerman
* Ref renamed `CacheFlushTimeout` to `InitCacheFlushTimeout` (#638) @lucas-zimerman
* Add support for performance. ([#633](https://github.com/getsentry/sentry-dotnet/pull/633))
* Transaction (of type `string`) on Scope and Event now is called TransactionName. ([#633](https://github.com/getsentry/sentry-dotnet/pull/633))

## 3.0.0-alpha.6

* Abandon ValueTask #611
* Fix Cache deleted on HttpTransport exception. (#610) @lucas-zimerman
* Add `SentryScopeStateProcessor` #603
* Add net5.0 TFM to libraries #606
* Add more logging to CachingTransport #619
* Bump Microsoft.Bcl.AsyncInterfaces to 5.0.0 #618
* Bump `Microsoft.Bcl.AsyncInterfaces` to 5.0.0 #618
* `DefaultTags` moved from `SentryLoggingOptions` to `SentryOptions` (#637) @PureKrome
* `Sentry.Serilog` can accept DefaultTags (#637) @PureKrome

## 3.0.0-alpha.5

* Replaced `BaseScope` with `IScope`. (#590) @Tyrrrz
* Removed code coverage report from the test folder. (#592) @lucas-zimerman
* Add target framework NET5.0 on Sentry.csproj. Change the type of `Extra` where value parameter become nullable. @lucas-zimerman
* Implement envelope caching. (#576) @Tyrrrz
* Add a list of .NET Frameworks installed when available. (#531) @lucas-zimerman
* Parse Mono and IL2CPP stacktraces for Unity and Xamarin (#578) @bruno-garcia
* Update TFMs and dependency min version (#580) @bruno-garcia
* Run all tests on .NET 5 (#583) @bruno-garcia

## 3.0.0-alpha.4

* Add the client user ip if both SendDefaultPii and IsEnvironmentUser are set. (#1015) @lucas-zimerman
* Replace Task with ValueTask where possible. (#564) @Tyrrrz
* Add support for ASP.NET Core gRPC (#563) @Mitch528
* Push API docs to GitHub Pages GH Actions (#570) @bruno-garcia
* Refactor envelopes

## 3.0.0-alpha.3

* Add support for user feedback. (#559) @lucas-zimerman
* Add support for envelope deserialization (#558) @Tyrrrz
* Add package description and tags to Sentry.AspNet @Tyrrrz
* Fix internal url references for the new Sentry documentation. (#562) @lucas-zimerman

## 3.0.0-alpha.2

* Set the Environment setting to 'production' if none was provided. (#550) @PureKrome
* ASPNET.Core hosting environment is set to 'production' / 'development' (notice lower casing) if no custom options.Enviroment is set. (#554) @PureKrome
* Add most popular libraries to InAppExclude #555 (@bruno-garcia)
* Add support for individual rate limits.
* Extend `SentryOptions.BeforeBreadcrumb` signature to accept returning nullable values.
* Add support for envelope deserialization.

## 3.0.0-alpha.1

* Rename `LogEntry` to `SentryMessage`. Change type of `SentryEvent.Message` from `string` to `SentryMessage`.
* Change the type of `Gpu.VendorId` from `int` to `string`.
* Add support for envelopes.
* Publishing symbols package (snupkg) to nuget.org with sourcelink

## 3.0.0-alpha.0

* Move aspnet-classic integration to Sentry.AspNet (#528) @Tyrrrz
* Merge Sentry.Protocol into Sentry (#527) @Tyrrrz
* Framework and runtime info (#526) @bruno-garcia
* Add NRTS to Sentry.Extensions.Logging (#524) @Tyrrrz
* Add NRTs to Sentry.Serilog, Sentry.NLog, Sentry.Log4Net (#521) @Tyrrrz
* Add NRTs to Sentry.AspNetCore (#520) @Tyrrrz
* Fix CI build on GitHub Actions (#523) @Tyrrrz
* Add GitHubActionsTestLogger (#511) @Tyrrrz

We'd love to get feedback.

## 2.2.0-alpha

Add nullable reference types support (Sentry, Sentry.Protocol) (#509)
fix: Use ASP.NET Core endpoint FQDN (#485)
feat: Add integration to TaskScheduler.UnobservedTaskException (#481)

## 2.1.6

fix: aspnet fqdn (#485) @bruno-garcia
ref: wait on test the time needed (#484) @bruno-garcia
feat: Add integration to TaskScheduler.UnobservedTaskException (#481) @lucas-zimerman
build(deps): bump Serilog.AspNetCore from 3.2.0 to 3.4.0 (#477)  @dependabot-preview
Fix README typo (#480) @AndreasLangberg
build(deps): bump coverlet.msbuild from 2.8.1 to 2.9.0 (#462) @dependabot-preview
build(deps): bump Microsoft.Extensions.Logging.Debug @dependabot-preview
fix some spelling (#475) @SimonCropp
build(deps): bump Microsoft.Extensions.Configuration.Json (#467) @dependabot-preview

## 2.1.5

* fix: MEL don't init if enabled (#460) @bruno-garcia
* feat: Device Calendar, Timezone, CultureInfo (#457) @bruno-garcia
* ref: Log out debug disabled (#459) @bruno-garcia
* dep: Bump PlatformAbstractions (#458) @bruno-garcia
* feat: Exception filter (#456) @bruno-garcia

## 2.1.5-beta

* fix: MEL don't init if enabled (#460) @bruno-garcia
* feat: Device Calendar, Timezone, CultureInfo (#457) @bruno-garcia
* ref: Log out debug disabled (#459) @bruno-garcia
* dep: Bump PlatformAbstractions (#458) @bruno-garcia
* feat: Exception filter (#456) @bruno-garcia

## 2.1.4

* NLog SentryTarget - NLogDiagnosticLogger for writing to NLog InternalLogger (#450) @snakefoot
* fix: SentryScopeManager dispose message (#449) @bruno-garcia
* fix: dont use Sentry namespace on sample (#447) @bruno-garcia
* Remove obsolete API from benchmarks (#445) @bruno-garcia
* build(deps): bump Microsoft.Extensions.Logging.Debug from 2.1.1 to 3.1.4 (#421) @dependabot-preview
* build(deps): bump Microsoft.AspNetCore.Diagnostics from 2.1.1 to 2.2.0 (#431) @dependabot-preview
* build(deps): bump Microsoft.CodeAnalysis.CSharp.Workspaces from 3.1.0 to 3.6.0 (#437) @dependabot-preview

## 2.1.3

* SentryScopeManager - Fixed clone of Stack so it does not reverse order (#420) @snakefoot
* build(deps): bump Serilog.AspNetCore from 2.1.1 to 3.2.0 (#411) @dependabot-preview
* Removed dependency on System.Collections.Immutable (#405) @snakefoot
* Fix Sentry.Microsoft.Logging Filter now drops also breadcrumbs (#440)

## 2.1.2-beta5

Fix Background worker dispose logs error message (#408)
Fix sentry serilog extension method collapsing (#406)
Fix Sentry.Samples.NLog so NLog.config is valid (#404)

Thanks @snakefoot and @JimHume for the fixes

Add MVC route data extraction to ScopeExtensions.Populate() (#401)

## 2.1.2-beta3

Fixed ASP.NET System.Web catch HttpException to prevent the request processor from being unable to submit #397 (#398)

## 2.1.2-beta2

* Ignore WCF error and capture (#391)

### 2.1.2-beta

* Serilog Sentry sink does not load all options from IConfiguration (#380)
* UnhandledException sets Handled=false (#382)

## 2.1.1

Bug fix:  Don't overwrite server name set via configuration with machine name on ASP.NET Core #372

## 2.1.0

* Set score url to fully constructed url #367 Thanks @christopher-taormina-zocdoc
* Don't dedupe from inner exception #363 - Note this might change groupings. It's opt-in.
* Expose FlushAsync to intellisense #362
* Protocol monorepo #325 - new protocol version whenever there's a new SDK release

## 2.0.3

Expose httpHandler creation (#359)
NLog: possibility to override fingerprint using AdditionalGroupingKey (#358) @Shtannikov
Take ServerName from options (#356)

## 2.0.2

Add logger and category from Serilog SourceContext. (#316) @krisztiankocsis
Set DateFormatHandling.IsoDateFormat for serializer. Fixes #351 (#353)  @olsh

## 2.0.1

Removed `-beta` from dependencies.

## 2.0.0

* SentryTarget - GetTagsFromLogEvent with null check (#326)
* handled process corrupted (#328)
* sourcelink GA (#330)
* Adds ability to specify user values via NLog configuration (#336)
* Add option to ASP.NET Core to flush events after response complete (#288)
* Fixed race on `BackgroundWorker`  (#293)
* Exclude `Sentry.` frames from InApp (#272)
* NLog SentryTarget with less overhead for breadcrumb (#273)
* Logging on body not extracted (#246)
* Add support to DefaultTags for ASP.NET Core and M.E.Logging (#268)
* Don't use ValueTuple (#263)
* All public members were documented: #252
* Use EnableBuffering to keep request payload around: #250
* Serilog default levels: #237
* Removed dev dependency from external dependencies 4d92ab0
* Use new `Sentry.Protocol` 836fb07e
* Use new `Sentry.PlatformAbsrtractions` #226
* Debug logging for ASP.NET Classic #209
* Reading request body throws on ASP.NET Core 3 (#324)
* NLog: null check contextProp.Value during IncludeEventDataOnBreadcrumbs (#323)
* JsonSerializerSettings - ReferenceLoopHandling.Ignore (#312)
* Fixed error when reading request body affects collecting other request data (#299)
* `Microsoft.Extensions.Logging` `ConfigureScope` invocation. #208, #210, #224 Thanks @dbraillon
* `Sentry.Serilog` Verbose level. #213, #217. Thanks @kanadaj
* AppDomain.ProcessExit will close the SDK: #242
* Adds PublicApiAnalyzers to public projects: #234
* NLog: Utilizes Flush functionality in NLog target: #228
* NLog: Set the logger via the log event info in SentryTarget.Write, #227
* Multi-target .NET Core 3.0 (#308)

Major version bumped due to these breaking changes:
1. `Sentry.Protocol` version 2.0.0
* Remove StackTrace from SentryEvent [#38](https://github.com/getsentry/sentry-dotnet-protocol/pull/38) - StackTrace is either part of Thread or SentryException.
2. Removed `ContextLine` #223
3. Use `StackTrace` from `Threads` #222
4. `FlushAsync` added to `ISentryClient` #214

## 2.0.0-beta8

* SentryTarget - GetTagsFromLogEvent with null check (#326)
* handled process corrupted (#328)
* sourcelink GA (#330)
* Adds ability to specify user values via NLog configuration (#336)

## 2.0.0-beta7

Fixes:

* Reading request body throws on ASP.NET Core 3 (#324)
* NLog: null check contextProp.Value during IncludeEventDataOnBreadcrumbs (#323)
* JsonSerializerSettings - ReferenceLoopHandling.Ignore (#312)

Features:

* Multi-target .NET Core 3.0 (#308)

## 2.0.0-beta6

* Fixed error when reading request body affects collecting other request data (#299)

## 2.0.0-beta5

* Add option to ASP.NET Core to flush events after response complete (#288)
* Fixed race on `BackgroundWorker`  (#293)
* Exclude `Sentry.` frames from InApp (#272)
* NLog SentryTarget with less overhead for breadcrumb (#273)

## 2.0.0-beta4

* Logging on body not extracted (#246)
* Add support to DefaultTags for ASP.NET Core and M.E.Logging (#268)
* Don't use ValueTuple (#263)

## 2.0.0-beta3

* All public members were documented: #252
* Use EnableBuffering to keep request payload around: #250
* Serilog default levels: #237

Thanks @josh-degraw for:

* AppDomain.ProcessExit will close the SDK: #242
* Adds PublicApiAnalyzers to public projects: #234
* NLog: Utilizes Flush functionality in NLog target: #228
* NLog: Set the logger via the log event info in SentryTarget.Write, #227

## 2.0.0-beta2

* Removed dev dependency from external dependencies 4d92ab0
* Use new `Sentry.Protocol` 836fb07e
* Use new `Sentry.PlatformAbsrtractions` #226

## 2.0.0-beta

Major version bumped due to these breaking changes:

1. `Sentry.Protocol` version 2.0.0
* Remove StackTrace from SentryEvent [#38](https://github.com/getsentry/sentry-dotnet-protocol/pull/38) - StackTrace is either part of Thread or SentryException.
2. Removed `ContextLine` #223
3. Use `StackTrace` from `Threads` #222
4. `FlushAsync` added to `ISentryClient` #214


Other Features:

* Debug logging for ASP.NET Classic #209

Fixes:

* `Microsoft.Extensions.Logging` `ConfigureScope` invocation. #208, #210, #224 Thanks @dbraillon
* `Sentry.Serilog` Verbose level. #213, #217. Thanks @kanadaj

## 1.2.1-beta

Fixes and improvements to the NLog integration: #207 by @josh-degraw

## 1.2.0

### Features

* Optionally skip module registrations #202 - (Thanks @josh-degraw)
* First NLog integration release #188 (Thanks @josh-degraw)
* Extensible stack trace #184 (Thanks @pengweiqhca)
* MaxRequestSize for ASP.NET and ASP.NET Core #174
* InAppInclude #171
* Overload to AddSentry #163 by (Thanks @f1nzer)
* ASP.NET Core AddSentry has now ConfigureScope: #160

### Bug fixes

* Don't override user #199
* Read the hub to take latest Client: 8f4b5ba

## 1.1.3-beta4

Bug fix: Don't override user  #199

## 1.1.3-beta3

* First NLog integration release #188 (Thanks @josh-degraw)
* Extensible stack trace #184 (Thanks @pengweiqhca)

## 1.1.3-beta2

Feature:
* MaxRequestSize for ASP.NET and ASP.NET Core #174
* InAppInclude #171

Fix: Diagnostic log order: #173 by @scolestock

## 1.1.3-beta

Fixed:
* Read the hub to take latest Client: 8f4b5ba1a3
* Uses Sentry.Protocol 1.0.4 4035e25

Feature
* Overload to `AddSentry` #163 by @F1nZeR
* ASP.NET Core `AddSentry` has now `ConfigureScope`: #160

## 1.1.2

Using [new version of the protocol with fixes and features](https://github.com/getsentry/sentry-dotnet-protocol/releases/tag/1.0.3).

Fixed:

ASP.NET Core integration issue when containers are built on the ServiceCollection after SDK is initialized (#157, #103 )

## 1.1.2-beta

Fixed:
* ASP.NET Core integration issue when containers are built on the ServiceCollection after SDK is initialized (#157, #103 )

## 1.1.1

Fixed:
* Serilog bug that self log would recurse #156

Feature:
* log4net environment via xml configuration #150 (Thanks Sébastien Pierre)

## 1.1.0

Includes all features and bug fixes of previous beta releases:

Features:

* Use log entry to improve grouping #125
* Use .NET Core SDK 2.1.401
* Make AddProcessors extension methods on Options public #115
* Format InternalsVisibleTo to avoid iOS issue: 94e28b3
* Serilog Integration #118, #145
* Capture methods return SentryId #139, #140
* MEL integration keeps properties as tags #146
* Sentry package Includes net461 target #135

Bug fixes:

* Disabled SDK throws on shutdown: #124
* Log4net only init if current hub is disabled #119

Thanks to our growing list of [contributors](https://github.com/getsentry/sentry-dotnet/graphs/contributors).

## 1.0.1-beta5

* Added `net461` target to Serilog package #148

## 1.0.1-beta4

* Serilog Integration #118, #145
* `Capture` methods return `SentryId` #139, #140
* MEL integration keeps properties as tags #146
* Revert reducing Json.NET requirements https://github.com/getsentry/sentry-dotnet/commit/1aed4a5c76ead2f4d39f1c2979eda02d068bfacd

Thanks to our growing [list of contributors](https://github.com/getsentry/sentry-dotnet/graphs/contributors).

## 1.0.1-beta3

Lowering Newtonsoft.Json requirements; #138

## 1.0.1-beta2

`Sentry` package Includes `net461` target #135

## 1.0.1-beta

Features:
* Use log entry to improve grouping #125
* Use .NET Core SDK 2.1.401
* Make `AddProcessors` extension methods on Options public  #115
* Format InternalsVisibleTo to avoid iOS issue: 94e28b3

Bug fixes:
* Disabled SDK throws on shutdown: #124
* Log4net only init if current hub is disabled #119

## 1.0.0

### First major release of the new .NET SDK.

#### Main features

##### Sentry package

* Automatic Captures global unhandled exceptions (AppDomain)
* Scope management
* Duplicate events automatically dropped
* Events from the same exception automatically dropped
* Web proxy support
* HttpClient/HttpClientHandler configuration callback
* Compress request body
* Event sampling opt-in
* Event flooding protection (429 retry-after and internal bound queue)
* Release automatically set (AssemblyInformationalVersionAttribute, AssemblyVersion or env var)
* DSN discovered via environment variable
* Release (version) reported automatically
* CLS Compliant
* Strong named
* BeforeSend and BeforeBreadcrumb callbacks
* Event and Exception processors
* SourceLink (including PDB in nuget package)
* Device OS info sent
* Device Runtime info sent
* Enable SDK debug mode (opt-in)
* Attach stack trace for captured messages (opt-in)

##### Sentry.Extensions.Logging

* Includes all features from the `Sentry` package.
* BeginScope data added to Sentry scope, sent with events
* LogInformation or higher added as breadcrumb, sent with next events.
* LogError or higher automatically captures an event
* Minimal levels are configurable.

##### Sentry.AspNetCore

* Includes all features from the `Sentry` package.
* Includes all features from the `Sentry.Extensions.Logging` package.
* Easy ASP.NET Core integration, single line: `UseSentry`.
* Captures unhandled exceptions in the middleware pipeline
* Captures exceptions handled by the framework `UseExceptionHandler` and Error page display.
* Any event sent will include relevant application log messages
* RequestId as tag
* URL as tag
* Environment is automatically set (`IHostingEnvironment`)
* Request payload can be captured if opt-in
* Support for EventProcessors registered with DI
* Support for ExceptionProcessors registered with DI
* Captures logs from the request (using Microsoft.Extensions.Logging)
* Supports configuration system (e.g: appsettings.json)
* Server OS info sent
* Server Runtime info sent
* Request headers sent
* Request body compressed

All packages are:
* Strong named
* Tested on Windows, Linux and macOS
* Tested on .NET Core, .NET Framework and Mono

##### Learn more:

* [Code samples](https://github.com/getsentry/sentry-dotnet/tree/master/samples)
* [Sentry docs](https://docs.sentry.io/quickstart/?platform=csharp)

Sample event using the log4net integration:
![Sample event in Sentry](https://github.com/getsentry/sentry-dotnet/blob/master/samples/Sentry.Samples.Log4Net/.assets/log4net-sample.gif?raw=true)

Download it directly from GitHub or using NuGet:

|      Integrations                 |        NuGet         |
| ----------------------------- | -------------------: |
|         **Sentry**            |    [![NuGet](https://img.shields.io/nuget/vpre/Sentry.svg)](https://www.nuget.org/packages/Sentry)   |
|     **Sentry.AspNetCore**     |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   |
| **Sentry.Extensions.Logging** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   |
| **Sentry.Log4Net** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net)   |
# 1.0.0-rc2

Features and improvements:

* `SentrySdk.LastEventId` to get scoped id
* `BeforeBreadcrumb` to allow dropping or modifying a breadcrumb
* Event processors on scope #58
* Event processor as `Func<SentryEvent,SentryEvent>`

Bug fixes:

* #97 Sentry environment takes precedence over ASP.NET Core

Download it directly below from GitHub or using NuGet:

|      Integrations                 |        NuGet         |
| ----------------------------- | -------------------: |
|         **Sentry**            |    [![NuGet](https://img.shields.io/nuget/vpre/Sentry.svg)](https://www.nuget.org/packages/Sentry)   |
|     **Sentry.AspNetCore**     |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   |
| **Sentry.Extensions.Logging** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   |
| **Sentry.Log4Net** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net)   |
# 1.0.0-rc

Features and improvements:

* Microsoft.Extensions.Logging (MEL) use framework configuration system #79 (Thanks @pengweiqhca)
* Use IOptions on Logging and ASP.NET Core integrations #81
* Send PII (personal identifier info, opt-in `SendDefaultPii`): #83
* When SDK is disabled SentryMiddleware passes through to next in pipeline: #84
* SDK diagnostic logging (option: `Debug`): #85
* Sending Stack trace for events without exception (like CaptureMessage, opt-in `AttachStackTrace`) #86

Bug fixes:

* MEL: Only call Init if DSN was provided https://github.com/getsentry/sentry-dotnet/commit/097c6a9c6f4348d87282c92d9267879d90879e2a
* Correct namespace for `AddSentry` https://github.com/getsentry/sentry-dotnet/commit/2498ab4081f171dc78e7f74e4f1f781a557c5d4f

Breaking changes:

The settings for HTTP and Worker have been moved to `SentryOptions`. There's no need to call `option.Http(h => h...)` anymore.
`option.Proxy` was renamed to `option.HttpProxy`.

[New sample](https://github.com/getsentry/sentry-dotnet/tree/master/samples/Sentry.Samples.GenericHost) using [GenericHost](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-2.1)

Download it directly below from GitHub or using NuGet:

|      Integrations                 |        NuGet         |
| ----------------------------- | -------------------: |
|         **Sentry**            |    [![NuGet](https://img.shields.io/nuget/vpre/Sentry.svg)](https://www.nuget.org/packages/Sentry)   |
|     **Sentry.AspNetCore**     |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   |
| **Sentry.Extensions.Logging** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   |
| **Sentry.Log4Net** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net)   |
# 0.0.1-preview5

Features:

* Support buffered gzip request #73
* Reduced dependencies from the ASP.NET Core integraiton
* InAppExclude configurable #75
* Duplicate event detects inner exceptions #76
* HttpClientHandler configuration callback #72
* Event sampling opt-in
* ASP.NET Core sends server name

Bug fixes:

* On-prem without chuncked support for gzip #71
* Exception.Data key is not string #77

##### [Watch on youtube](https://www.youtube.com/watch?v=xK6a1goK_w0) how to use the ASP.NET Core integration.

Download it directly below from GitHub or using NuGet:

|      Integrations                 |        NuGet         |
| ----------------------------- | -------------------: |
|         **Sentry**            |    [![NuGet](https://img.shields.io/nuget/vpre/Sentry.svg)](https://www.nuget.org/packages/Sentry)   |
|     **Sentry.AspNetCore**     |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   |
| **Sentry.Extensions.Logging** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   |
| **Sentry.Log4Net** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net)   |

## 0.0.1-preview4

Features:

* Using [Sentry Protocol](https://github.com/getsentry/sentry-dotnet-protocol) as a dependency
* Environment can be set via `SentryOptions` #49
* Compress request body (configurable: Fastest, Optimal, Off) #63
* log4net integration
* SDK honors Sentry's 429 HTTP Status with Retry After header #61

Bug fixes:

* `Init` pushes the first scope #55, #54
* `Exception.Data` copied to `SentryEvent.Data` while storing the index of originating error.
* Demangling code ensures Function name available #64
* ASP.NET Core integration throws when Serilog added #65, #68, #67

Improvements to [the docs](https://getsentry.github.io/sentry-dotnet) like:
* Release discovery
* `ConfigureScope` clarifications
* Documenting samples

### [Watch on youtube](https://www.youtube.com/watch?v=xK6a1goK_w0) how to use the ASP.NET Core integration.

Download it directly from GitHub or using NuGet:

|      Integrations                 |        NuGet         |
| ----------------------------- | -------------------: |
|         **Sentry**            |    [![NuGet](https://img.shields.io/nuget/vpre/Sentry.svg)](https://www.nuget.org/packages/Sentry)   |
|     **Sentry.AspNetCore**     |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   |
| **Sentry.Extensions.Logging** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   |
| **Sentry.Log4Net** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net)   |

## 0.0.1-preview3

This third preview includes bug fixes and more features. Test coverage increased to 96%

Features and improvements:

* Filter duplicate events/exceptions #43
* EventProcessors can be added (sample [1](https://github.com/getsentry/sentry-dotnet/blob/dbb5a3af054d0ca6f801de37fb7db3632ca2c65a/samples/Sentry.Samples.Console.Customized/Program.cs#L151), [2](https://github.com/getsentry/sentry-dotnet/blob/dbb5a3af054d0ca6f801de37fb7db3632ca2c65a/samples/Sentry.Samples.Console.Customized/Program.cs#L41))
* ExceptionProcessors can be added #36 (sample [1](https://github.com/getsentry/sentry-dotnet/blob/dbb5a3af054d0ca6f801de37fb7db3632ca2c65a/samples/Sentry.Samples.Console.Customized/Program.cs#L172), [2](https://github.com/getsentry/sentry-dotnet/blob/dbb5a3af054d0ca6f801de37fb7db3632ca2c65a/samples/Sentry.Samples.Console.Customized/Program.cs#L42))
* Release is automatically discovered/reported #35
* Contexts is a dictionary - allows custom data #37
* ASP.NET integration reports context as server: server-os, server-runtime #37
* Assemblies strong named #41
* Scope exposes IReadOnly members instead of Immutables
* Released a [documentation site](https://getsentry.github.io/sentry-dotnet/)

Bug fixes:

#46 Strong name
#40 Logger provider gets disposed/flushes events

[Watch on youtube](https://www.youtube.com/watch?v=xK6a1goK_w0) how to use the ASP.NET Core integration.

Download it directly from GitHub or using NuGet:

|      Integrations                 |        NuGet         |
| ----------------------------- | -------------------: |
|         **Sentry**            |    [![NuGet](https://img.shields.io/nuget/vpre/Sentry.svg)](https://www.nuget.org/packages/Sentry)   |
|     **Sentry.AspNetCore**     |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   |
| **Sentry.Extensions.Logging** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   |
## 0.0.1-preview2

This second release includes bug fixes and more features. Test coverage increased to 93%

Features and improvements:
* Added `CaptureMessage`
* `BeforeSend` callback errors are sent as breadcrumbs
* `ASP.NET Core` integration doesn't add tags added by `Microsoft.Extensions.Logging`
* SDK name is reported depending on the package added
* Integrations API allows user-defined SDK integration
* Unhandled exception handler can be configured via integrations
* Filter kestrel log eventid 13 (application error) when already captured by the middleware

Bugs fixed:
* Fixed #28
* HTTP Proxy set to HTTP message handler

Download it directly from GitHub or using NuGet:

|      Integrations                 |        NuGet         |
| ----------------------------- | -------------------: |
|         **Sentry**            |    [![NuGet](https://img.shields.io/nuget/vpre/Sentry.svg)](https://www.nuget.org/packages/Sentry)   |
|     **Sentry.AspNetCore**     |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   |
| **Sentry.Extensions.Logging** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   |

## 0.0.1-preview1

Our first preview of the SDK:

Main features:
* Easy ASP.NET Core integration, single line: `UseSentry`.
* Captures unhandled exceptions in the middleware pipeline
* Captures exceptions handled by the framework `UseExceptionHandler` and Error page display.
* Captures process-wide unhandled exceptions (AppDomain)
* Captures logger.Error or logger.Critical
* When an event is sent, data from the current request augments the event.
* Sends information about the server running the app (OS, Runtime, etc)
* Informational logs written by the app or framework augment events sent to Sentry
* Optional include of the request body
* HTTP Proxy configuration

Also available via NuGet:

[Sentry](https://www.nuget.org/packages/Sentry/0.0.1-preview1)
[Sentry.AspNetCore](https://www.nuget.org/packages/Sentry.AspNetCore/0.0.1-preview1)
[Sentry.Extensions.Logging](https://www.nuget.org/packages/Sentry.Extensions.Logging/0.0.1-preview1)
