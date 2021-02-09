# Changelog

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
