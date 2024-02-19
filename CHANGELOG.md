# Changelog

## Unreleased

### Fixes

- Metric unit names are now sanitized correctly. This was preventing some built in metrics from showing in the Sentry dashboard ([#3151](https://github.com/getsentry/sentry-dotnet/pull/3151))
- The Sentry OpenTelemetry integration no longer throws an exception with the SDK disabled ([#3156](https://github.com/getsentry/sentry-dotnet/pull/3156))

## 4.1.1

### Fixes

- The SDK can be disabled by setting `options.Dsn = "";` By convention, the SDK allows the DSN set to `string.Empty` to be overwritten by the environment. ([#3147](https://github.com/getsentry/sentry-dotnet/pull/3147))

### Dependencies

- Bump CLI from v2.28.0 to v2.28.6 ([#3145](https://github.com/getsentry/sentry-dotnet/pull/3145), [#3148](https://github.com/getsentry/sentry-dotnet/pull/3148))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2286)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.28.0...2.28.6)

## 4.1.0

### Features

- The SDK now automatically collects metrics coming from `OpenTelemetry.Instrumentation.Runtime` ([#3133](https://github.com/getsentry/sentry-dotnet/pull/3133))

### Fixes

- "No service for type 'Sentry.IHub' has been registered" exception when using OpenTelemetry and initializing Sentry via `SentrySdk.Init` ([#3129](https://github.com/getsentry/sentry-dotnet/pull/3129))

## 4.0.3

### Fixes

- To resolve conflicting types due to the SDK adding itself to the global usings: 
  - The class `Sentry.Constants` has been renamed to `Sentry.SentryConstants` ([#3125](https://github.com/getsentry/sentry-dotnet/pull/3125))

## 4.0.2

### Fixes

- To resolve conflicting types due to the SDK adding itself to the global usings: 
  - The class `Sentry.Context` has been renamed to `Sentry.SentryContext` ([#3121](https://github.com/getsentry/sentry-dotnet/pull/3121))
  - The class `Sentry.Package` has been renamed to `Sentry.SentryPackage` ([#3121](https://github.com/getsentry/sentry-dotnet/pull/3121))
  - The class `Sentry.Request` has been renamed to `Sentry.SentryRequest` ([#3121](https://github.com/getsentry/sentry-dotnet/pull/3121))

### Dependencies

- Bump CLI from v2.27.0 to v2.28.0 ([#3119](https://github.com/getsentry/sentry-dotnet/pull/3119))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2280)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.27.0...2.28.0)

## 4.0.1

### Fixes 

- To resolve conflicting types due to the SDK adding itself to the global usings: 
  - The interface `Sentry.ISession` has been renamed to `Sentry.ISentrySession` ([#3110](https://github.com/getsentry/sentry-dotnet/pull/3110))
  - The interface `Sentry.IJsonSerializable` has been renamed to `Sentry.ISentryJsonSerializable` ([#3116](https://github.com/getsentry/sentry-dotnet/pull/3116))
  - The class `Sentry.Session` has been renamed to `Sentry.SentrySession` ([#3110](https://github.com/getsentry/sentry-dotnet/pull/3110))
  - The class `Sentry.Attachment` has been renamed to `Sentry.SentryAttachment` ([#3116](https://github.com/getsentry/sentry-dotnet/pull/3116))
  - The class `Sentry.Hint` has been renamed to `Sentry.SentryHint` ([#3116](https://github.com/getsentry/sentry-dotnet/pull/3116))

### Dependencies

- Bump Cocoa SDK from v8.19.0 to v8.20.0 ([#3107](https://github.com/getsentry/sentry-dotnet/pull/3107))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8200)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.19.0...8.20.0)

## 4.0.0

This major release includes many exciting new features including support for [Profiling](https://docs.sentry.io/platforms/dotnet/profiling/) and [Metrics](https://docs.sentry.io/platforms/dotnet/metrics/)(preview), [AOT](https://sentry.engineering/blog/should-you-could-you-aot) with [Native Crash Reporting](https://github.com/getsentry/sentry-dotnet/issues/2770), [Spotlight](https://spotlightjs.com/), Screenshots on MAUI and much more. Details about these features and other changes are below.

### .NET target framework changes

We're dropping support for some of the old target frameworks, please check this [GitHub Discussion](https://github.com/getsentry/sentry-dotnet/discussions/2776) for details on why.

- **Replace support for .NET Framework 4.6.1 with 4.6.2** ([#2786](https://github.com/getsentry/sentry-dotnet/pull/2786))

  .NET Framework 4.6.1 was announced on Nov 30, 2015. And went out of support over a year ago, on Apr 26, 2022.

- **Drop .NET Core 3.1 and .NET 5 support** ([#2787](https://github.com/getsentry/sentry-dotnet/pull/2787))

- **Dropped netstandard2.0 support for Sentry.AspNetCore** ([#2807](https://github.com/getsentry/sentry-dotnet/pull/2807))

- **Replace support for .NET 6 on mobile (e.g: `net6.0-android`) with .NET 7** ([#2624](https://github.com/getsentry/sentry-dotnet/pull/2604))

  .NET 6 on mobile has been out of support since May 2023 and with .NET 8, it's no longer possible to build .NET 6 Mobile specific targets.
  For that reason, we're moving the mobile-specific TFMs from `net6.0-platform` to `net7.0-platform`.

  Mobile apps still work on .NET 6 will pull the `Sentry` .NET 6, which offers the .NET-only features,
  without native/platform-specific bindings and SDKs. See [this ticket for more details](https://github.com/getsentry/sentry-dotnet/issues/2623).

- **MAUI dropped Tizen support** ([#2734](https://github.com/getsentry/sentry-dotnet/pull/2734))

### Sentry Self-hosted Compatibility

If you're using `sentry.io` this change does not affect you.
This SDK version is compatible with a self-hosted version of Sentry `22.12.0` or higher. If you are using an older version of [self-hosted Sentry](https://develop.sentry.dev/self-hosted/) (aka on-premise), you will need to [upgrade](https://develop.sentry.dev/self-hosted/releases/). 

### Significant change in behavior

- Transaction names for ASP.NET Core are now consistently named `HTTP-VERB /path` (e.g. `GET /home`). Previously, the leading forward slash was missing for some endpoints. ([#2808](https://github.com/getsentry/sentry-dotnet/pull/2808))
- Setting `SentryOptions.Dsn` to `null` now throws `ArgumentNullException` during initialization. ([#2655](https://github.com/getsentry/sentry-dotnet/pull/2655))
- Enable `CaptureFailedRequests` by default ([#2688](https://github.com/getsentry/sentry-dotnet/pull/2688))
- Added `Sentry` namespace to global usings when `ImplicitUsings` is enabled ([#3043](https://github.com/getsentry/sentry-dotnet/pull/3043))
If you have conflicts, you can opt out by adding the following to your `csproj`:
```
<PropertyGroup>
  <SentryImplicitUsings>false</SentryImplicitUsings>
</PropertyGroup>
```
- Transactions' spans are no longer automatically finished with the status `deadline_exceeded` by the transaction. This is now handled by the [Relay](https://github.com/getsentry/relay). 
  - Customers self hosting Sentry must use verion 22.12.0 or later ([#3013](https://github.com/getsentry/sentry-dotnet/pull/3013))
- The `User.IpAddress` is now set to `{{auto}}` by default, even when sendDefaultPII is disabled ([#2981](https://github.com/getsentry/sentry-dotnet/pull/2981))
  - The "Prevent Storing of IP Addresses" option in the "Security & Privacy" project settings on sentry.io can be used to control this instead
- The `DiagnosticLogger` signature for `LogWarning` changed to take the `exception` as the first parameter. That way it no longer gets mixed up with the TArgs. ([#2987](https://github.com/getsentry/sentry-dotnet/pull/2987))

### API breaking Changes

If you have compilation errors you can find the affected types or overloads missing in the changelog entries below.

#### Changed APIs

- Class renamed `Sentry.User` to `Sentry.SentryUser` ([#3015](https://github.com/getsentry/sentry-dotnet/pull/3015))
- Class renamed `Sentry.Runtime` to `Sentry.SentryRuntime` ([#3016](https://github.com/getsentry/sentry-dotnet/pull/3016))
- Class renamed `Sentry.Span` to `Sentry.SentrySpan` ([#3021](https://github.com/getsentry/sentry-dotnet/pull/3021))
- Class renamed `Sentry.Transaction` to `Sentry.SentryTransaction` ([#3023](https://github.com/getsentry/sentry-dotnet/pull/3023))
- Rename iOS and MacCatalyst platform-specific options from `Cocoa` to `Native` ([#2940](https://github.com/getsentry/sentry-dotnet/pull/2940))
- Rename iOS platform-specific options `EnableCocoaSdkTracing` to `EnableTracing` ([#2940](https://github.com/getsentry/sentry-dotnet/pull/2940))
- Rename Android platform-specific options from `Android` to `Native` ([#2940](https://github.com/getsentry/sentry-dotnet/pull/2940))
- Rename Android platform-specific options `EnableAndroidSdkTracing` and `EnableAndroidSdkBeforeSend` to `EnableTracing` and `EnableBeforeSend` respectively ([#2940](https://github.com/getsentry/sentry-dotnet/pull/2940))
- Rename iOS and MacCatalyst platform-specific options from `iOS` to `Cocoa` ([#2929](https://github.com/getsentry/sentry-dotnet/pull/2929))
- `ITransaction` has been renamed to `ITransactionTracer`. You will need to update any references to these interfaces in your code to use the new interface names ([#2731](https://github.com/getsentry/sentry-dotnet/pull/2731), [#2870](https://github.com/getsentry/sentry-dotnet/pull/2870))
- `DebugImage` and `DebugMeta` moved to `Sentry.Protocol` namespace. ([#2815](https://github.com/getsentry/sentry-dotnet/pull/2815))
- `SentryClient.Dispose` is no longer obsolete ([#2842](https://github.com/getsentry/sentry-dotnet/pull/2842))
- `ISentryClient.CaptureEvent` overloads have been replaced by a single method accepting optional `Hint` and `Scope` parameters. You will need to pass `hint` as a named parameter from code that calls `CaptureEvent` without passing a `scope` argument. ([#2749](https://github.com/getsentry/sentry-dotnet/pull/2749))
- `TransactionContext` and `SpanContext` constructors were updated. If you're constructing instances of these classes, you will need to adjust the order in which you pass parameters to these. ([#2694](https://github.com/getsentry/sentry-dotnet/pull/2694), [#2696](https://github.com/getsentry/sentry-dotnet/pull/2696))
- The `DiagnosticLogger` signature for `LogError` and `LogFatal` changed to take the `exception` as the first parameter. That way it no longer gets mixed up with the TArgs. The `DiagnosticLogger` now also receives an overload for `LogError` and `LogFatal` that accepts a message only. ([#2715](https://github.com/getsentry/sentry-dotnet/pull/2715))
- `Distribution` added to `IEventLike`. ([#2660](https://github.com/getsentry/sentry-dotnet/pull/2660))
- `StackFrame`'s `ImageAddress`, `InstructionAddress`, and `FunctionId` changed to `long?`. ([#2691](https://github.com/getsentry/sentry-dotnet/pull/2691))
- `DebugImage.ImageAddress` changed to `long?`. ([#2725](https://github.com/getsentry/sentry-dotnet/pull/2725))
- Contexts now inherit from `IDictionary` rather than `ConcurrentDictionary`. The specific dictionary being used is an implementation detail. ([#2729](https://github.com/getsentry/sentry-dotnet/pull/2729))
- The method used to configure a Sentry Sink for Serilog now has an additional overload. Calling `WriteTo.Sentry()` with no arguments will no longer attempt to initialize the SDK (it has optional arguments to configure the behavior of the Sink only). If you want to initialize Sentry at the same time you configure the Sentry Sink then you will need to use the overload of this method that accepts a DSN as the first parameter (e.g. `WriteTo.Sentry("https://d4d82fc1c2c4032a83f3a29aa3a3aff@fake-sentry.io:65535/2147483647")`). ([#2928](https://github.com/getsentry/sentry-dotnet/pull/2928))

#### Removed APIs

- SentrySinkExtensions.ConfigureSentrySerilogOptions is now internal. If you were using this method, please use one of the `SentrySinkExtensions.Sentry` extension methods instead. ([#2902](https://github.com/getsentry/sentry-dotnet/pull/2902))
- A number of `[Obsolete]` options have been removed ([#2841](https://github.com/getsentry/sentry-dotnet/pull/2841))
  - `BeforeSend` - use `SetBeforeSend` instead.
  - `BeforeSendTransaction` - use `SetBeforeSendTransaction` instead.
  - `BeforeBreadcrumb` - use `SetBeforeBreadcrumb` instead.
  - `CreateHttpClientHandler` - use `CreateHttpMessageHandler` instead.
  - `ReportAssemblies` - use `ReportAssembliesMode` instead.
  - `KeepAggregateException` - this property is no longer used and has no replacement.
  - `DisableTaskUnobservedTaskExceptionCapture` method has been renamed to `DisableUnobservedTaskExceptionCapture`.
  - `DebugDiagnosticLogger` - use `TraceDiagnosticLogger` instead.
- A number of iOS/Android-specific `[Obsolete]` options have been removed ([#2856](https://github.com/getsentry/sentry-dotnet/pull/2856))
  - `Distribution` - use `SentryOptions.Distribution` instead.
  - `EnableAutoPerformanceTracking` - use `SetBeforeSendTransaction` instead.
  - `EnableCoreDataTracking` - use `EnableCoreDataTracing` instead.
  - `EnableFileIOTracking` - use `EnableFileIOTracing` instead.
  - `EnableOutOfMemoryTracking` - use `EnableWatchdogTerminationTracking` instead.
  - `EnableUIViewControllerTracking` - use `EnableUIViewControllerTracing` instead.
  - `StitchAsyncCode` - no longer available.
  - `ProfilingTracesInterval` - no longer available.
  - `ProfilingEnabled` - use `ProfilesSampleRate` instead.
- Obsolete `SystemClock` constructor removed, use `SystemClock.Clock` instead. ([#2856](https://github.com/getsentry/sentry-dotnet/pull/2856))
- Obsolete `Runtime.Clone()` removed, this shouldn't have been public in the past and has no replacement. ([#2856](https://github.com/getsentry/sentry-dotnet/pull/2856))
- Obsolete `SentryException.Data` removed, use `SentryException.Mechanism.Data` instead. ([#2856](https://github.com/getsentry/sentry-dotnet/pull/2856))
- Obsolete `AssemblyExtensions` removed, this shouldn't have been public in the past and has no replacement. ([#2856](https://github.com/getsentry/sentry-dotnet/pull/2856))
- Obsolete `SentryDatabaseLogging.UseBreadcrumbs()` removed, it is called automatically and has no replacement. ([#2856](https://github.com/getsentry/sentry-dotnet/pull/2856))
- Obsolete `Scope.GetSpan()` removed, use `Span` property instead. ([#2856](https://github.com/getsentry/sentry-dotnet/pull/2856))
- Obsolete `IUserFactory` removed, use `ISentryUserFactory` instead. ([#2856](https://github.com/getsentry/sentry-dotnet/pull/2856), [#2840](https://github.com/getsentry/sentry-dotnet/pull/2840))
- `IHasMeasurements` has been removed, use `ISpanData` instead. ([#2659](https://github.com/getsentry/sentry-dotnet/pull/2659))
- `IHasBreadcrumbs` has been removed, use `IEventLike` instead. ([#2670](https://github.com/getsentry/sentry-dotnet/pull/2670))
- `ISpanContext` has been removed, use `ITraceContext` instead. ([#2668](https://github.com/getsentry/sentry-dotnet/pull/2668))
- `IHasTransactionNameSource` has been removed, use `ITransactionContext` instead. ([#2654](https://github.com/getsentry/sentry-dotnet/pull/2654))
- ([#2694](https://github.com/getsentry/sentry-dotnet/pull/2694))
- The unused `StackFrame.InstructionOffset` has been removed. ([#2691](https://github.com/getsentry/sentry-dotnet/pull/2691))
- The unused `Scope.Platform` property has been removed. ([#2695](https://github.com/getsentry/sentry-dotnet/pull/2695))
- The obsolete setter `Sentry.PlatformAbstractions.Runtime.Identifier` has been removed ([2764](https://github.com/getsentry/sentry-dotnet/pull/2764))
- `Sentry.Values<T>` is now internal as it is never exposed in the public API ([#2771](https://github.com/getsentry/sentry-dotnet/pull/2771))
- The `TracePropagationTarget` class has been removed, use the `SubstringOrRegexPattern` class instead. ([#2763](https://github.com/getsentry/sentry-dotnet/pull/2763))
- The `WithScope` and `WithScopeAsync` methods have been removed. We have discovered that these methods didn't work correctly in certain desktop contexts, especially when using a global scope. ([#2717](https://github.com/getsentry/sentry-dotnet/pull/2717))

  Replace your usage of `WithScope` with overloads of `Capture*` methods:

  - `SentrySdk.CaptureEvent(SentryEvent @event, Action<Scope> scopeCallback)`
  - `SentrySdk.CaptureMessage(string message, Action<Scope> scopeCallback)`
  - `SentrySdk.CaptureException(Exception exception, Action<Scope> scopeCallback)`

  ```c#
  // Before
  SentrySdk.WithScope(scope =>
  {
    scope.SetTag("key", "value");
    SentrySdk.CaptureEvent(new SentryEvent());
  });

  // After
  SentrySdk.CaptureEvent(new SentryEvent(), scope =>
  {
    // Configure your scope here
    scope.SetTag("key", "value");
  });
  ```

### Features

- Experimental pre-release availability of Metrics. We're exploring the use of Metrics in Sentry. The API will very likely change and we don't yet have any documentation. ([#2949](https://github.com/getsentry/sentry-dotnet/pull/2949))
  - `SentrySdk.Metrics.Set` now additionally accepts `string` as value ([#3092](https://github.com/getsentry/sentry-dotnet/pull/3092))
  - Timing metrics can now be captured with `SentrySdk.Metrics.StartTimer` ([#3075](https://github.com/getsentry/sentry-dotnet/pull/3075))
  - Added support for capturing built-in metrics from the `System.Diagnostics.Metrics` API ([#3052](https://github.com/getsentry/sentry-dotnet/pull/3052))
- `Sentry.Profiling` is now available as a package on [nuget](nuget.org). Be aware that profiling is in alpha and on servers the overhead could be high. Improving the experience for ASP.NET Core is tracked on [this issue](
https://github.com/getsentry/sentry-dotnet/issues/2316) ([#2800](https://github.com/getsentry/sentry-dotnet/pull/2800))
  - iOS profiling support (alpha). ([#2930](https://github.com/getsentry/sentry-dotnet/pull/2930))
- Native crash reporting on NativeAOT published apps (Windows, Linux, macOS). ([#2887](https://github.com/getsentry/sentry-dotnet/pull/2887))
- Support for [Spotlight](https://spotlightjs.com/), a debug tool for local development. ([#2961](https://github.com/getsentry/sentry-dotnet/pull/2961))
  - Enable it with the option `EnableSpotlight`
  - Optionally configure the URL to connect via `SpotlightUrl`. Defaults to `http://localhost:8969/stream`.

### MAUI

- Added screenshot capture support for errors. You can opt-in via `SentryMauiOptions.AttachScreenshots` ([#2965](https://github.com/getsentry/sentry-dotnet/pull/2965))
  - Supports Android and iOS only. Windows is not supported.
- App context now has `in_foreground`, indicating whether the app was in the foreground or the background. ([#2983](https://github.com/getsentry/sentry-dotnet/pull/2983))
- Android: When capturing unhandled exceptions, the SDK now can automatically attach `LogCat` to the event. You can opt-in via `SentryOptions.Android.LogCatIntegration` and configure `SentryOptions.Android.LogCatMaxLines`. ([#2926](https://github.com/getsentry/sentry-dotnet/pull/2926))
  - Available when targeting `net7.0-android` or later, on API level 23 or later.

#### Native AOT

Native AOT publishing support for .NET 8 has been added to Sentry for the following platforms:

- Windows
- Linux
- macOS
- Mac Catalyst
- iOS

There are some functional differences when publishing Native AOT:

- `StackTraceMode.Enhanced` is ignored because it's not available when publishing Native AOT. The mechanism to generate these enhanced stack traces relies heavily on reflection which isn't compatible with trimming.
- Reflection cannot be leveraged for JSON Serialization and you may need to use `SentryOptions.AddJsonSerializerContext` to supply a serialization context for types that you'd like to send to Sentry (e.g. in the `Span.Context`). ([#2732](https://github.com/getsentry/sentry-dotnet/pull/2732), [#2793](https://github.com/getsentry/sentry-dotnet/pull/2793))
- `Ben.Demystifier` is not available as it only runs in JIT mode.
- WinUI applications: When publishing Native AOT, Sentry isn't able to automatically register an unhandled exception handler because that relies on reflection. You'll need to [register the unhandled event handler manually](https://github.com/getsentry/sentry-dotnet/issues/2778) instead.
- For Azure Functions Workers, when AOT/Trimming is enabled we can't use reflection to read route data from the HttpTrigger so the route name will always be `/api/<FUNCTION_NAME>` ([#2920](https://github.com/getsentry/sentry-dotnet/pull/2920))

### Fixes

- Native integration logging on macOS ([#3079](https://github.com/getsentry/sentry-dotnet/pull/3079))
- The scope transaction is now correctly set for Otel transactions ([#3072](https://github.com/getsentry/sentry-dotnet/pull/3072))
- Fixed an issue with tag values in metrics not being properly serialized ([#3065](https://github.com/getsentry/sentry-dotnet/pull/3065))
- Moved the binding to MAUI events for breadcrumb creation from `WillFinishLaunching` to `FinishedLaunching`. This delays the initial instantiation of `app`. ([#3057](https://github.com/getsentry/sentry-dotnet/pull/3057))
- The SDK no longer adds the `WinUIUnhandledExceptionIntegration` on non-Windows platforms ([#3055](https://github.com/getsentry/sentry-dotnet/pull/3055))
- Stop Sentry for MacCatalyst from creating `default.profraw` in the app bundle using xcodebuild archive to build sentry-cocoa ([#2960](https://github.com/getsentry/sentry-dotnet/pull/2960))
- Workaround a .NET 8 NativeAOT crash on transaction finish. ([#2943](https://github.com/getsentry/sentry-dotnet/pull/2943))
- Reworked automatic breadcrumb creation for MAUI. ([#2900](https://github.com/getsentry/sentry-dotnet/pull/2900))
  - The SDK no longer uses reflection to bind to all public element events. This also fixes issues where the SDK would consume third-party events.
  - Added `CreateElementEventsBreadcrumbs` to the SentryMauiOptions to allow users to opt-in automatic breadcrumb creation for `BindingContextChanged`, `ChildAdded`, `ChildRemoved`, and `ParentChanged` on `Element`.
  - Reduced amount of automatic breadcrumbs by limiting the number of bindings created in `VisualElement`, `Window`, `Shell`, `Page`, and `Button`.
- Fixed Sentry SDK has not been initialized when using ASP.NET Core, Serilog, and OpenTelemetry ([#2911](https://github.com/getsentry/sentry-dotnet/pull/2911))
- Android native symbol upload ([#2876](https://github.com/getsentry/sentry-dotnet/pull/2876))
- `Sentry.Serilog` no longer throws if a disabled DSN is provided when initializing Sentry via the Serilog integration ([#2883](https://github.com/getsentry/sentry-dotnet/pull/2883))
- Don't add WinUI exception integration on mobile platforms ([#2821](https://github.com/getsentry/sentry-dotnet/pull/2821))
- `Transactions` are now getting enriched by the client instead of the hub ([#2838](https://github.com/getsentry/sentry-dotnet/pull/2838))
- Fixed an issue when using the SDK together with OpenTelemetry `1.5.0` and newer where the SDK would create transactions for itself. The fix is backward compatible. ([#3001](https://github.com/getsentry/sentry-dotnet/pull/3001))

### Dependencies

- Upgraded to NLog version 5. ([#2697](https://github.com/getsentry/sentry-dotnet/pull/2697))
- Integrate `sentry-native` as a static library in Native AOT builds to enable symbolication. ([#2704](https://github.com/getsentry/sentry-dotnet/pull/2704))


- Bump Cocoa SDK from v8.16.1 to v8.19.0 ([#2910](https://github.com/getsentry/sentry-dotnet/pull/2910), [#2936](https://github.com/getsentry/sentry-dotnet/pull/2936), [#2972](https://github.com/getsentry/sentry-dotnet/pull/2972), [#3005](https://github.com/getsentry/sentry-dotnet/pull/3005), [#3084](https://github.com/getsentry/sentry-dotnet/pull/3084))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8190)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.16.1...8.19.0)
- Bump Java SDK from v6.34.0 to v7.3.0 ([#2932](https://github.com/getsentry/sentry-dotnet/pull/2932), [#2979](https://github.com/getsentry/sentry-dotnet/pull/2979), [#3049](https://github.com/getsentry/sentry-dotnet/pull/3049), (https://github.com/getsentry/sentry-dotnet/pull/3098))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#730)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.34.0...7.3.0)
- Bump Native SDK from v0.6.5 to v0.6.7 ([#2914](https://github.com/getsentry/sentry-dotnet/pull/2914), [#3029](https://github.com/getsentry/sentry-dotnet/pull/3029))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#070)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.6.5...0.7.0)
- Bump CLI from v2.21.5 to v2.27.0 ([#2901](https://github.com/getsentry/sentry-dotnet/pull/2901), [#2915](https://github.com/getsentry/sentry-dotnet/pull/2915), [#2956](https://github.com/getsentry/sentry-dotnet/pull/2956), [#2985](https://github.com/getsentry/sentry-dotnet/pull/2985), [#2999](https://github.com/getsentry/sentry-dotnet/pull/2999), [#3012](https://github.com/getsentry/sentry-dotnet/pull/3012), [#3030](https://github.com/getsentry/sentry-dotnet/pull/3030), [#3059](https://github.com/getsentry/sentry-dotnet/pull/3059), [#3062](https://github.com/getsentry/sentry-dotnet/pull/3062), [#3073](https://github.com/getsentry/sentry-dotnet/pull/3073), [#3099](https://github.com/getsentry/sentry-dotnet/pull/3099))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2270)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.21.5...2.27.0)

## 3.41.4

### Fixes

- Fixed an issue when using the SDK together with Open Telemetry `1.5.0` and newer where the SDK would create transactions for itself. The fix is backward compatible. ([#3001](https://github.com/getsentry/sentry-dotnet/pull/3001))

## 3.41.3

### Fixes

- Fixed Sentry SDK has not been initialised when using ASP.NET Core, Serilog, and OpenTelemetry ([#2918](https://github.com/getsentry/sentry-dotnet/pull/2918))

## 3.41.2

### Fixes

- The SDK no longer fails to finish sessions while capturing an event. This fixes broken crash-free rates ([#2895](https://github.com/getsentry/sentry-dotnet/pull/2895))
- Ignore UnobservedTaskException for QUIC exceptions. See: https://github.com/dotnet/runtime/issues/80111 ([#2894](https://github.com/getsentry/sentry-dotnet/pull/2894))

### Dependencies

- Bump Cocoa SDK from v8.16.0 to v8.16.1 ([#2891](https://github.com/getsentry/sentry-dotnet/pull/2891))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8161)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.16.0...8.16.1)

## 3.41.1

### Fixes

- `CaptureFailedRequests` and `FailedRequestStatusCodes` are now getting respected by the Cocoa SDK. This is relevant for MAUI apps where requests are getting handled natively. ([#2826](https://github.com/getsentry/sentry-dotnet/issues/2826))
- Added `SentryOptions.AutoRegisterTracing` for users who need to control registration of Sentry's tracing middleware ([#2871](https://github.com/getsentry/sentry-dotnet/pull/2871))

### Dependencies

- Bump Cocoa SDK from v8.15.0 to v8.16.0 ([#2812](https://github.com/getsentry/sentry-dotnet/pull/2812), [#2816](https://github.com/getsentry/sentry-dotnet/pull/2816), [#2882](https://github.com/getsentry/sentry-dotnet/pull/2882))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8160)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.15.0...8.16.0)
- Bump CLI from v2.21.2 to v2.21.5 ([#2811](https://github.com/getsentry/sentry-dotnet/pull/2811), [#2834](https://github.com/getsentry/sentry-dotnet/pull/2834), [#2851](https://github.com/getsentry/sentry-dotnet/pull/2851))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2215)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.21.2...2.21.5)
- Bump Java SDK from v6.33.1 to v6.34.0 ([#2874](https://github.com/getsentry/sentry-dotnet/pull/2874))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6340)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.33.1...6.34.0)

## 3.41.0

### Features

- Speed up SDK init ([#2784](https://github.com/getsentry/sentry-dotnet/pull/2784))

### Fixes

- Fixed chaining on the IApplicationBuilder for methods like UseRouting and UseEndpoints ([#2726](https://github.com/getsentry/sentry-dotnet/pull/2726))

### Dependencies

- Bump Cocoa SDK from v8.13.0 to v8.15.0 ([#2722](https://github.com/getsentry/sentry-dotnet/pull/2722), [#2740](https://github.com/getsentry/sentry-dotnet/pull/2740), [#2746](https://github.com/getsentry/sentry-dotnet/pull/2746), [#2801](https://github.com/getsentry/sentry-dotnet/pull/2801))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8150)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.13.0...8.15.0)
- Bump Java SDK from v6.30.0 to v6.33.1 ([#2723](https://github.com/getsentry/sentry-dotnet/pull/2723), [#2741](https://github.com/getsentry/sentry-dotnet/pull/2741), [#2783](https://github.com/getsentry/sentry-dotnet/pull/2783), [#2803](https://github.com/getsentry/sentry-dotnet/pull/2803))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6331)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.30.0...6.33.1)

## 3.40.1

### Fixes

- ISentryUserFactory is now public so users can register their own implementations via DI ([#2719](https://github.com/getsentry/sentry-dotnet/pull/2719))

## 3.40.0

### Obsoletion

- `WithScope` and `WithScopeAsync` have been proven to not work correctly in desktop contexts when using a global scope. They are now deprecated in favor of the overloads of `CaptureEvent`, `CaptureMessage`, and `CaptureException`. Those methods provide a callback to a configurable scope. ([#2677](https://github.com/getsentry/sentry-dotnet/pull/2677))
- `StackFrame.InstructionOffset` has not been used in the SDK and has been ignored on the server for years. ([#2689](https://github.com/getsentry/sentry-dotnet/pull/2689))

### Features

- Release of Azure Functions (Isolated Worker/Out-of-Process) support ([#2686](https://github.com/getsentry/sentry-dotnet/pull/2686))

### Fixes

- Scope is now correctly applied to Transactions when using OpenTelemetry on ASP.NET Core ([#2690](https://github.com/getsentry/sentry-dotnet/pull/2690))

### Dependencies

- Bump CLI from v2.20.7 to v2.21.2 ([#2645](https://github.com/getsentry/sentry-dotnet/pull/2645), [#2647](https://github.com/getsentry/sentry-dotnet/pull/2647), [#2698](https://github.com/getsentry/sentry-dotnet/pull/2698))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2212)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.20.7...2.21.2)
- Bump Cocoa SDK from v8.12.0 to v8.13.0 ([#2653](https://github.com/getsentry/sentry-dotnet/pull/2653))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8130)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.12.0...8.13.0)
- Bump Java SDK from v6.29.0 to v6.30.0 ([#2685](https://github.com/getsentry/sentry-dotnet/pull/2685))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6300)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.29.0...6.30.0)

## 3.40.0-beta.0

### Features

- Reduced the memory footprint of `SpanId` by refactoring the ID generation ([#2619](https://github.com/getsentry/sentry-dotnet/pull/2619))
- Reduced the memory footprint of `SpanTracer` by initializing the tags lazily ([#2636](https://github.com/getsentry/sentry-dotnet/pull/2636))
- Added distributed tracing without performance for Azure Function Workers ([#2630](https://github.com/getsentry/sentry-dotnet/pull/2630))
- The SDK now provides and overload of `ContinueTrace` that accepts headers as `string` ([#2601](https://github.com/getsentry/sentry-dotnet/pull/2601))
- Sentry tracing middleware now gets configured automatically ([#2602](https://github.com/getsentry/sentry-dotnet/pull/2602))
- Added memory optimisations for GetLastActiveSpan ([#2642](https://github.com/getsentry/sentry-dotnet/pull/2642))

### Fixes

- Resolved issue identifying users with OpenTelemetry ([#2618](https://github.com/getsentry/sentry-dotnet/pull/2618))

### Azure Functions Beta

- Package name changed from `Sentry.AzureFunctions.Worker` to `Sentry.Azure.Functions.Worker`. Note AzureFunctions now is split by a `.`. ([#2637](https://github.com/getsentry/sentry-dotnet/pull/2637))

### Dependencies

- Bump CLI from v2.20.6 to v2.20.7 ([#2604](https://github.com/getsentry/sentry-dotnet/pull/2604))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2207)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.20.6...2.20.7)
- Bump Cocoa SDK from v8.11.0 to v8.12.0 ([#2640](https://github.com/getsentry/sentry-dotnet/pull/2640))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8120)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.11.0...8.12.0)

## 3.39.1

### Fixes

- Added Sentry.AspNet.csproj back to Sentry-CI-Build-macOS.slnf ([#2612](https://github.com/getsentry/sentry-dotnet/pull/2612))

## 3.39.0

### Features

- Added additional `DB` attributes to automatically generated spans like `name` and `provider` ([#2583](https://github.com/getsentry/sentry-dotnet/pull/2583))
- `Hints` now accept attachments provided as a file path via `AddAttachment` method ([#2585](https://github.com/getsentry/sentry-dotnet/pull/2585))

### Fixes

- Resolved an isse where the SDK would throw an exception while attempting to set the DynamicSamplingContext but the context exists already. ([#2592](https://github.com/getsentry/sentry-dotnet/pull/2592))

### Dependencies

- Bump CLI from v2.20.5 to v2.20.6 ([#2590](https://github.com/getsentry/sentry-dotnet/pull/2590))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2206)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.20.5...2.20.6)
- Bump Cocoa SDK from v8.10.0 to v8.11.0 ([#2594](https://github.com/getsentry/sentry-dotnet/pull/2594))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8110)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.10.0...8.11.0)
- Bump Java SDK from v6.28.0 to v6.29.0 ([#2599](https://github.com/getsentry/sentry-dotnet/pull/2599))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6290)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.28.0...6.29.0)

## 3.36.0

### Features

- Graphql client ([#2538](https://github.com/getsentry/sentry-dotnet/pull/2538))

### Fixes

- Android: Fix proguard/r8 mapping file upload ([#2574](https://github.com/getsentry/sentry-dotnet/pull/2574))

### Dependencies

- Bump Cocoa SDK from v8.9.5 to v8.10.0 ([#2546](https://github.com/getsentry/sentry-dotnet/pull/2546), [#2550](https://github.com/getsentry/sentry-dotnet/pull/2550))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8100)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.9.5...8.10.0)
- Bump gradle/gradle-build-action from 2.7.0 to 2.7.1 ([#2564](https://github.com/getsentry/sentry-dotnet/pull/2564))
  - [diff](https://github.com/gradle/gradle-build-action/compare/v2.7.0...v2.7.1)

## 3.35.1

### Fixes

- The SDK no longer creates transactions with their start date set to `Jan 01, 001` ([#2544](https://github.com/getsentry/sentry-dotnet/pull/2544))

### Dependencies

- Bump CLI from v2.20.4 to v2.20.5 ([#2539](https://github.com/getsentry/sentry-dotnet/pull/2539))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2205)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.20.4...2.20.5)
- Bump Cocoa SDK from v8.9.4 to v8.9.5 ([#2542](https://github.com/getsentry/sentry-dotnet/pull/2542))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#895)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.9.4...8.9.5)

## 3.35.0

### Features

- Distributed tracing now works independently of the performance feature. This allows you to connect errors to other Sentry instrumented applications ([#2493](https://github.com/getsentry/sentry-dotnet/pull/2493))
- Added Sampling Decision to Trace Envelope Header ([#2495](https://github.com/getsentry/sentry-dotnet/pull/2495))
- Add MinimumEventLevel to Sentry.Log4Net and convert events below it to breadcrumbs ([#2505](https://github.com/getsentry/sentry-dotnet/pull/2505))
- Support transaction finishing automatically with 'idle timeout' (#2452)

### Fixes

- Fixed baggage propagation when an exception is thrown from middleware ([#2487](https://github.com/getsentry/sentry-dotnet/pull/2487))
- Fix Durable Functions preventing orchestrators from completing ([#2491](https://github.com/getsentry/sentry-dotnet/pull/2491))
- Re-enable HubTests.FlushOnDispose_SendsEnvelope ([#2492](https://github.com/getsentry/sentry-dotnet/pull/2492))
- Fixed SDK not sending exceptions via Blazor WebAssembly due to a `PlatformNotSupportedException` ([#2506](https://github.com/getsentry/sentry-dotnet/pull/2506))
- Align SDK with docs regarding session update for dropped events ([#2496](https://github.com/getsentry/sentry-dotnet/pull/2496))
- Introduced `HttpMessageHandler` in favor of the now deprecated `HttpClientHandler` on the options. This allows the SDK to support NSUrlSessionHandler on iOS ([#2503](https://github.com/getsentry/sentry-dotnet/pull/2503))
- Using `Activity.RecordException` now correctly updates the error status of OpenTelemetry Spans ([#2515](https://github.com/getsentry/sentry-dotnet/pull/2515))
- Fixed Transaction name not reporting correctly when using UseExceptionHandler ([#2511](https://github.com/getsentry/sentry-dotnet/pull/2511))
- log4net logging Level.All now maps to SentryLevel.Debug ([#2522]([url](https://github.com/getsentry/sentry-dotnet/pull/2522)))

### Dependencies

- Bump Java SDK from v6.25.1 to v6.28.0 ([#2484](https://github.com/getsentry/sentry-dotnet/pull/2484), [#2498](https://github.com/getsentry/sentry-dotnet/pull/2498), [#2517](https://github.com/getsentry/sentry-dotnet/pull/2517), [#2533](https://github.com/getsentry/sentry-dotnet/pull/2533))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6280)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.25.1...6.28.0)
- Bump CLI from v2.19.4 to v2.20.4 ([#2509](https://github.com/getsentry/sentry-dotnet/pull/2509), [#2518](https://github.com/getsentry/sentry-dotnet/pull/2518), [#2527](https://github.com/getsentry/sentry-dotnet/pull/2527), [#2530](https://github.com/getsentry/sentry-dotnet/pull/2530))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2204)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.19.4...2.20.4)
- Bump Cocoa SDK from v8.8.0 to v8.9.4 ([#2479](https://github.com/getsentry/sentry-dotnet/pull/2479), [#2483](https://github.com/getsentry/sentry-dotnet/pull/2483), [#2500](https://github.com/getsentry/sentry-dotnet/pull/2500), [#2510](https://github.com/getsentry/sentry-dotnet/pull/2510), [#2531](https://github.com/getsentry/sentry-dotnet/pull/2531))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#894)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.8.0...8.9.4)

## 3.34.0

### Features

- OpenTelemetry Support ([#2453](https://github.com/getsentry/sentry-dotnet/pull/2453))
- Added a MSBuild property `SentryUploadAndroidProguardMapping` to automatically upload the Proguard mapping file when targeting Android ([#2455](https://github.com/getsentry/sentry-dotnet/pull/2455))
- Symbolication for Single File Apps ([#2425](https://github.com/getsentry/sentry-dotnet/pull/2425))
- Add binding to `SwiftAsyncStacktraces` on iOS ([#2436](https://github.com/getsentry/sentry-dotnet/pull/2436))

### Fixes

- Builds targeting Android with `r8` enabled no longer crash during SDK init. The package now contains the required proguard rules ([#2450](https://github.com/getsentry/sentry-dotnet/pull/2450))
- Fix Sentry logger options for MAUI and Azure Functions ([#2423](https://github.com/getsentry/sentry-dotnet/pull/2423))

### Dependencies

- Bump Cocoa SDK from v8.7.3 to v8.8.0 ([#2427](https://github.com/getsentry/sentry-dotnet/pull/2427), [#2430](https://github.com/getsentry/sentry-dotnet/pull/2430))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#880)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.7.3...8.8.0)
- Bump CLI from v2.18.1 to v2.19.4 ([#2428](https://github.com/getsentry/sentry-dotnet/pull/2428), [#2431](https://github.com/getsentry/sentry-dotnet/pull/2431), [#2451](https://github.com/getsentry/sentry-dotnet/pull/2451), [#2454](https://github.com/getsentry/sentry-dotnet/pull/2454))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2194)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.18.1...2.19.4)
- Bump Java SDK from v6.22.0 to v6.25.1 ([#2429](https://github.com/getsentry/sentry-dotnet/pull/2429), [#2440](https://github.com/getsentry/sentry-dotnet/pull/2440), [#2458](https://github.com/getsentry/sentry-dotnet/pull/2458), [#2476](https://github.com/getsentry/sentry-dotnet/pull/2476))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6251)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.22.0...6.25.1)

## 3.33.1

### Fixes

- SentryHttpMessageHandler added when AddHttpClient is before UseSentry ([#2390](https://github.com/getsentry/sentry-dotnet/pull/2390))
- Set the native sdk name for Android ([#2389](https://github.com/getsentry/sentry-dotnet/pull/2389))
- Fix db connection spans not finishing ([#2398](https://github.com/getsentry/sentry-dotnet/pull/2398))
- Various .NET MAUI fixes / improvements ([#2403](https://github.com/getsentry/sentry-dotnet/pull/2403))
  - The battery level was being reported incorrectly due to percentage multiplier.
  - The device architecture (x64, arm64, etc.) is now reported
  - On Windows, the OS type is now reported as "Windows" instead of "WinUI".  Additionally, the OS display version (ex, "22H2") is now included.
  - `UIKit`, `ABI.Microsoft` and `WinRT`  frames are now marked "system" instead of "in app".
- Reduce debug files uploaded ([#2404](https://github.com/getsentry/sentry-dotnet/pull/2404))
- Fix system frames being marked as "in-app" ([#2408](https://github.com/getsentry/sentry-dotnet/pull/2408))
  - NOTE: This important fix corrects a value that is used during issue grouping, so you may receive new alerts for existing issues after deploying this update.
- DB Connection spans presented poorly ([#2409](https://github.com/getsentry/sentry-dotnet/pull/2409))
- Populate scope's Cookies property ([#2411](https://github.com/getsentry/sentry-dotnet/pull/2411))
- Fix UWP GateKeeper errors ([#2415](https://github.com/getsentry/sentry-dotnet/pull/2415))
- Fix sql client db name ([#2418](https://github.com/getsentry/sentry-dotnet/pull/2418))

### Dependencies

- Bump Cocoa SDK from v8.7.2 to v8.7.3 ([#2394](https://github.com/getsentry/sentry-dotnet/pull/2394))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#873)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.7.2...8.7.3)
- Bump Java SDK from v6.19.1 to v6.22.0 ([#2395](https://github.com/getsentry/sentry-dotnet/pull/2395), [#2405](https://github.com/getsentry/sentry-dotnet/pull/2405), [#2417](https://github.com/getsentry/sentry-dotnet/pull/2417))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6220)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.19.1...6.22.0)

## 3.33.0

### Features

- .NET SDK changes for exception groups ([#2287](https://github.com/getsentry/sentry-dotnet/pull/2287))
  - This changes how `AggregateException` is handled.  Instead of filtering them out client-side, the SDK marks them as an "exception group",
    and adds includes data that represents the hierarchical structure of inner exceptions. Sentry now recognizes this server-side,
    improving the accuracy of the issue detail page.
  - Accordingly, the `KeepAggregateException` option is now obsolete and does nothing.  Please remove any usages of `KeepAggregateException`.
  - NOTE: If running Self-Hosted Sentry, you should wait to adopt this SDK update until after updating to the 23.6.0 (est. June 2023) release of Sentry.
    The effect of updating the SDK early will be as if `KeepAggregateException = true` was set.  That will not break anything, but may affect issue grouping and alerts.

### Fixes

- Status messages when uploading symbols or sources are improved. ([#2307](https://github.com/getsentry/sentry-dotnet/issues/2307))

### Dependencies

- Bump CLI from v2.18.0 to v2.18.1 ([#2386](https://github.com/getsentry/sentry-dotnet/pull/2386))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2181)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.18.0...2.18.1)

## 3.32.0

### Features

- Azure Functions (Isolated Worker/Out-of-Process) support ([#2346](https://github.com/getsentry/sentry-dotnet/pull/2346))
  - Initial `beta.1` release.  Please give it a try and let us know how it goes!
  - Documentation is TBD.  For now, see `/samples/Sentry.Samples.Azure.Functions.Worker`.

- Add `Hint` support  ([#2351](https://github.com/getsentry/sentry-dotnet/pull/2351))
  - Currently, this allows you to manipulate attachments in the various "before" event delegates.
  - Hints can also be used in event and transaction processors by implementing `ISentryEventProcessorWithHint` or `ISentryTransactionProcessorWithHint`, instead of `ISentryEventProcessor` or `ISentryTransactionProcessor`.
  - Note: Obsoletes the `BeforeSend`, `BeforeSendTransaction`, and `BeforeBreadcrumb` properties on the `SentryOptions` class.  They have been replaced with `SetBeforeSend`, `SetBeforeSendTransaction`, and `SetBeforeBreadcrumb` respectively.  Each one provides overloads both with and without a `Hint` object.

- Allow setting the active span on the scope ([#2364](https://github.com/getsentry/sentry-dotnet/pull/2364))
  - Note: Obsoletes the `Scope.GetSpan` method in favor of a `Scope.Span` property (which now has a setter as well).

- Remove authority from URLs sent to Sentry ([#2365](https://github.com/getsentry/sentry-dotnet/pull/2365))
- Add tag filters to `SentryOptions` ([#2367](https://github.com/getsentry/sentry-dotnet/pull/2367))

### Fixes

- Fix `EnableTracing` option conflict with `TracesSampleRate` ([#2368](https://github.com/getsentry/sentry-dotnet/pull/2368))
  - NOTE: This is a potentially breaking change, as the `TracesSampleRate` property has been made nullable.
    Though extremely uncommon, if you are _retrieving_ the `TracesSampleRate` property for some reason, you will need to account for nulls.
    However, there is no change to the behavior or _typical_ usage of either of these properties.

- CachedTransport gracefully handles malformed envelopes during processing  ([#2371](https://github.com/getsentry/sentry-dotnet/pull/2371))
- Remove extraneous iOS simulator resources when building MAUI apps using Visual Studio "Hot Restart" mode, to avoid hitting Windows max path  ([#2384](https://github.com/getsentry/sentry-dotnet/pull/2384))

### Dependencies

- Bump Cocoa SDK from v8.6.0 to v8.7.1 ([#2359](https://github.com/getsentry/sentry-dotnet/pull/2359), [#2370](https://github.com/getsentry/sentry-dotnet/pull/2370))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#871)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.6.0...8.7.1)
- Bump Java SDK from v6.18.1 to v6.19.1 ([#2374](https://github.com/getsentry/sentry-dotnet/pull/2374), [#2381](https://github.com/getsentry/sentry-dotnet/pull/2381))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6191)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.18.1...6.19.1)
- Bump Cocoa SDK from v8.6.0 to v8.7.2 ([#2359](https://github.com/getsentry/sentry-dotnet/pull/2359), [#2370](https://github.com/getsentry/sentry-dotnet/pull/2370), [#2375](https://github.com/getsentry/sentry-dotnet/pull/2375))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#872)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.6.0...8.7.2)
- Bump CLI from v2.17.5 to v2.18.0 ([#2380](https://github.com/getsentry/sentry-dotnet/pull/2380))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2180)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.17.5...2.18.0)

## 3.31.0

### Features

- Initial work to support profiling in a future release. ([#2206](https://github.com/getsentry/sentry-dotnet/pull/2206))
- Create a Sentry event for failed HTTP requests ([#2320](https://github.com/getsentry/sentry-dotnet/pull/2320))
- Improve `WithScope` and add `WithScopeAsync` ([#2303](https://github.com/getsentry/sentry-dotnet/pull/2303)) ([#2309](https://github.com/getsentry/sentry-dotnet/pull/2309))
- Build .NET Standard 2.1 for Unity ([#2328](https://github.com/getsentry/sentry-dotnet/pull/2328))
- Add `RemoveExceptionFilter`, `RemoveEventProcessor` and `RemoveTransactionProcessor` extension methods on `SentryOptions` ([#2331](https://github.com/getsentry/sentry-dotnet/pull/2331))
- Include Dynamic Sampling Context with error events, when there's a transaction ([#2332](https://github.com/getsentry/sentry-dotnet/pull/2332))

### Fixes

- Buffer payloads asynchronously when appropriate ([#2297](https://github.com/getsentry/sentry-dotnet/pull/2297))
- Restore `System.Reflection.Metadata` dependency for .NET Core 3 ([#2302](https://github.com/getsentry/sentry-dotnet/pull/2302))
- Capture open transactions on disabled hubs ([#2319](https://github.com/getsentry/sentry-dotnet/pull/2319))
- Remove session breadcrumbs ([#2333](https://github.com/getsentry/sentry-dotnet/pull/2333))
- Support synchronous `HttpClient.Send` in `SentryHttpMessageHandler` ([#2336](https://github.com/getsentry/sentry-dotnet/pull/2336))
- Fix ASP.NET Core issue with missing context when using capture methods that configure scope ([#2339](https://github.com/getsentry/sentry-dotnet/pull/2339))
- Improve debug file upload handling ([#2349](https://github.com/getsentry/sentry-dotnet/pull/2349))

### Dependencies

- Bump CLI from v2.17.0 to v2.17.5 ([#2298](https://github.com/getsentry/sentry-dotnet/pull/2298), [#2318](https://github.com/getsentry/sentry-dotnet/pull/2318), [#2321](https://github.com/getsentry/sentry-dotnet/pull/2321), [#2345](https://github.com/getsentry/sentry-dotnet/pull/2345))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2175)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.17.0...2.17.5)
- Bump Cocoa SDK from v8.4.0 to v8.6.0 ([#2310](https://github.com/getsentry/sentry-dotnet/pull/2310), [#2344](https://github.com/getsentry/sentry-dotnet/pull/2344))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#860)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.4.0...8.6.0)
- Bump Java SDK from v6.17.0 to v6.18.1 ([#2338](https://github.com/getsentry/sentry-dotnet/pull/2338), [#2343](https://github.com/getsentry/sentry-dotnet/pull/2343))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6181)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.17.0...6.18.1)

## 3.30.0

### Features

- Add `FileDiagnosticLogger` to assist with debugging the SDK ([#2242](https://github.com/getsentry/sentry-dotnet/pull/2242))
- Attach stack trace when events have captured an exception without a stack trace ([#2266](https://github.com/getsentry/sentry-dotnet/pull/2266))
- Add `Scope.Clear` and `Scope.ClearBreadcrumbs` methods ([#2284](https://github.com/getsentry/sentry-dotnet/pull/2284))
- Improvements to exception mechanism data ([#2294](https://github.com/getsentry/sentry-dotnet/pull/2294))

### Fixes

- Normalize StackFrame in-app resolution for modules & function prefixes ([#2234](https://github.com/getsentry/sentry-dotnet/pull/2234))
- Calling `AddAspNet` more than once should not block all errors from being sent ([#2253](https://github.com/getsentry/sentry-dotnet/pull/2253))
- Fix Sentry CLI arguments when using custom URL or auth token parameters ([#2259](https://github.com/getsentry/sentry-dotnet/pull/2259))
- Sentry.AspNetCore fix transaction name when path base is used and route starts with a slash ([#2265](https://github.com/getsentry/sentry-dotnet/pull/2265))
- Fix Baggage header parsing in ASP.NET (Framework) ([#2293](https://github.com/getsentry/sentry-dotnet/pull/2293))

### Dependencies

- Bump Cocoa SDK from v8.3.0 to v8.4.0 ([#2237](https://github.com/getsentry/sentry-dotnet/pull/2237), [#2248](https://github.com/getsentry/sentry-dotnet/pull/2248), [#2251](https://github.com/getsentry/sentry-dotnet/pull/2251), [#2285](https://github.com/getsentry/sentry-dotnet/pull/2285))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#840)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.3.0...8.4.0)

- Bump CLI from v2.14.4 to v2.17.0 ([#2238](https://github.com/getsentry/sentry-dotnet/pull/2238), [#2244](https://github.com/getsentry/sentry-dotnet/pull/2244), [#2252](https://github.com/getsentry/sentry-dotnet/pull/2252), [#2264](https://github.com/getsentry/sentry-dotnet/pull/2264), [#2292](https://github.com/getsentry/sentry-dotnet/pull/2292))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2170)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.14.4...2.17.0)

- Bump Java SDK from v6.15.0 to v6.17.0 ([#2243](https://github.com/getsentry/sentry-dotnet/pull/2243), [#2277](https://github.com/getsentry/sentry-dotnet/pull/2277))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6170)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.15.0...6.17.0)

## 3.29.1

### Fixes

- Get debug image for Full PDB format on Windows ([#2222](https://github.com/getsentry/sentry-dotnet/pull/2222))
- Fix debug files not uploading for `packages.config` nuget ([#2224](https://github.com/getsentry/sentry-dotnet/pull/2224))

### Dependencies

- Bump Cocoa SDK from v8.2.0 to v8.3.0 ([#2220](https://github.com/getsentry/sentry-dotnet/pull/2220))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#830)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.2.0...8.3.0)

## 3.29.0

**Notice:** The `<SentryUploadSymbols>` MSBuild property previously defaulted to `true` for projects compiled in `Release` configuration.
It is now `false` by default.  To continue uploading symbols, you must opt-in by setting it to `true`.
See the [MSBuild Setup](https://docs.sentry.io/platforms/dotnet/configuration/msbuild/) docs for further details.

### Features

- Added basic functionality to support `View Hierarchy` ([#2163](https://github.com/getsentry/sentry-dotnet/pull/2163))
- Allow `SentryUploadSources` to work even when not uploading symbols ([#2197](https://github.com/getsentry/sentry-dotnet/pull/2197))
- Add support for `BeforeSendTransaction` ([#2188](https://github.com/getsentry/sentry-dotnet/pull/2188))
- Add `EnableTracing` option to simplify enabling tracing ([#2201](https://github.com/getsentry/sentry-dotnet/pull/2201))
- Make `SentryUploadSymbols` strictly opt-in ([#2216](https://github.com/getsentry/sentry-dotnet/pull/2216))

### Fixes

- Fix assembly not found on Android in Debug configuration ([#2175](https://github.com/getsentry/sentry-dotnet/pull/2175))
- Fix context object with circular reference prevents event from being sent ([#2210](https://github.com/getsentry/sentry-dotnet/pull/2210))

### Dependencies

- Bump Java SDK from v6.13.1 to v6.15.0 ([#2185](https://github.com/getsentry/sentry-dotnet/pull/2185), [#2207](https://github.com/getsentry/sentry-dotnet/pull/2207))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6150)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.13.1...6.15.0)
- Bump CLI from v2.12.0 to v2.14.4 ([#2187](https://github.com/getsentry/sentry-dotnet/pull/2187), [#2215](https://github.com/getsentry/sentry-dotnet/pull/2215))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2144)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.12.0...2.14.4)
- Bump Java SDK from v6.13.1 to v6.14.0 ([#2185](https://github.com/getsentry/sentry-dotnet/pull/2185))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6140)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.13.1...6.14.0)
- Bump CLI from v2.12.0 to v2.14.3 ([#2187](https://github.com/getsentry/sentry-dotnet/pull/2187), [#2208](https://github.com/getsentry/sentry-dotnet/pull/2208))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2143)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.12.0...2.14.3)
- Bump Cocoa SDK from v7.31.5 to v8.2.0 ([#2203](https://github.com/getsentry/sentry-dotnet/pull/2203))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#820)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.31.5...8.2.0)

## 3.28.1

### Fixes

- Fix MAUI missing breadcrumbs for lifecycle and UI events ([#2170](https://github.com/getsentry/sentry-dotnet/pull/2170))
- Fix hybrid sdk names ([#2171](https://github.com/getsentry/sentry-dotnet/pull/2171))
- Fix ASP.NET sdk name ([#2172](https://github.com/getsentry/sentry-dotnet/pull/2172))

## 3.28.0

### Features

- Added `instruction_addr_adjustment` attribute to SentryStackTrace ([#2151](https://github.com/getsentry/sentry-dotnet/pull/2151))

### Fixes

- Workaround Visual Studio "Pair to Mac" issue (on Windows), and Update bundled Cocoa SDK to version 7.31.5 ([#2164](https://github.com/getsentry/sentry-dotnet/pull/2164))
- Sentry SDK assemblies no longer have PDBs embedded. Debug symbols are uploaded to `nuget.org` as `snupkg` packages  ([#2166](https://github.com/getsentry/sentry-dotnet/pull/2166))

### Dependencies

- Bump Java SDK from v6.13.0 to v6.13.1 ([#2168](https://github.com/getsentry/sentry-dotnet/pull/2168))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6131)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.13.0...6.13.1)

## 3.27.1

### Fixes

- Fix Sentry CLI MSBuild for Xamarin and NetFX ([#2154](https://github.com/getsentry/sentry-dotnet/pull/2154))
- Log aborted HTTP requests as debug instead of error ([#2155](https://github.com/getsentry/sentry-dotnet/pull/2155))

## 3.27.0

### Features

- Publish `Sentry.Android.AssemblyReader` as a separate nuget package (for reuse by `Sentry.Xamarin`) ([#2127](https://github.com/getsentry/sentry-dotnet/pull/2127))
- Improvements for Sentry CLI integration ([#2145](https://github.com/getsentry/sentry-dotnet/pull/2145))
- Update bundled Android SDK to version 6.13.0 ([#2147](https://github.com/getsentry/sentry-dotnet/pull/2147))

## 3.26.2

### Fixes

- Fix Sentry CLI integration on Windows ([#2123](https://github.com/getsentry/sentry-dotnet/pull/2123)) ([#2124](https://github.com/getsentry/sentry-dotnet/pull/2124))

## 3.26.1

### Fixes

- Fix issue with Sentry CLI msbuild properties ([#2119](https://github.com/getsentry/sentry-dotnet/pull/2119))

## 3.26.0

### Features

- Use Sentry CLI after build to upload symbols ([#2107](https://github.com/getsentry/sentry-dotnet/pull/2107))

### Fixes

- Logging info instead of warning when skipping debug images ([#2101](https://github.com/getsentry/sentry-dotnet/pull/2101))
- Fix unhandled exception not captured when hub disabled ([#2103](https://github.com/getsentry/sentry-dotnet/pull/2103))
- Fix Android support for Portable PDB format when app uses split APKs ([#2108](https://github.com/getsentry/sentry-dotnet/pull/2108))
- Fix session ending as crashed for unobserved task exceptions ([#2112](https://github.com/getsentry/sentry-dotnet/pull/2112))
- Set absolute path when stripping project path on stack frame ([#2117](https://github.com/getsentry/sentry-dotnet/pull/2117))

## 3.25.0

### Features

- Add support for Portable PDB format ([#2050](https://github.com/getsentry/sentry-dotnet/pull/2050))
- Update bundled Android SDK to version 6.10.0([#2095](https://github.com/getsentry/sentry-dotnet/pull/2095))
- Update bundled Cocoa SDK to version 7.31.4 ([#2096](https://github.com/getsentry/sentry-dotnet/pull/2096))

### Fixes

- Fix db warnings caused by transaction sampled out ([#2097](https://github.com/getsentry/sentry-dotnet/pull/2097))

## 3.24.1

### Fixes

- Fix missing stack trace on UnobservedTaskException ([#2067](https://github.com/getsentry/sentry-dotnet/pull/2067))
- Fix warning caused by db connection span closed prematurely ([#2068](https://github.com/getsentry/sentry-dotnet/pull/2068))
- Attach db connections to child spans correctly ([#2071](https://github.com/getsentry/sentry-dotnet/pull/2071))
- Improve MAUI event bindings ([#2089](https://github.com/getsentry/sentry-dotnet/pull/2089))

## 3.24.0

### Features

- Simplify API for flushing events ([#2030](https://github.com/getsentry/sentry-dotnet/pull/2030))
- Update bundled Cocoa SDK to version 7.31.1 ([#2053](https://github.com/getsentry/sentry-dotnet/pull/2053))
- Update bundled Android SDK to version 6.7.1 ([#2058](https://github.com/getsentry/sentry-dotnet/pull/2058))

### Fixes

- Update unobserved task exception integration ([#2034](https://github.com/getsentry/sentry-dotnet/pull/2034))
- Fix trace propagation targets setter ([#2035](https://github.com/getsentry/sentry-dotnet/pull/2035))
- Fix DiagnosticSource integration disabled incorrectly with TracesSampler ([#2039](https://github.com/getsentry/sentry-dotnet/pull/2039))
- Update transitive dependencies to resolve security warnings ([#2045](https://github.com/getsentry/sentry-dotnet/pull/2045))
- Fix issue with Hot Restart for iOS ([#2047](https://github.com/getsentry/sentry-dotnet/pull/2047))
- Fix `CacheDirectoryPath` option on MAUI ([#2055](https://github.com/getsentry/sentry-dotnet/pull/2055))

## 3.23.1

### Fixes

- Fix concurrency bug in caching transport ([#2026](https://github.com/getsentry/sentry-dotnet/pull/2026))

## 3.23.0

### Features

- Update bundled Android SDK to version 6.5.0 ([#1984](https://github.com/getsentry/sentry-dotnet/pull/1984))
- Update bundled Cocoa SDK to version 7.28.0 ([#1988](https://github.com/getsentry/sentry-dotnet/pull/1988))
- Allow custom processors to be added as a scoped dependency ([#1979](https://github.com/getsentry/sentry-dotnet/pull/1979))
- Support DI for custom transaction processors ([#1993](https://github.com/getsentry/sentry-dotnet/pull/1993))
- Mark Transaction as aborted when unhandled exception occurs ([#1996](https://github.com/getsentry/sentry-dotnet/pull/1996))
- Build Windows and Tizen targets for `Sentry.Maui` ([#2005](https://github.com/getsentry/sentry-dotnet/pull/2005))
- Add Custom Measurements API ([#2013](https://github.com/getsentry/sentry-dotnet/pull/2013))
- Add `ISpan.GetTransaction` convenience method ([#2014](https://github.com/getsentry/sentry-dotnet/pull/2014))

### Fixes

- Split Android and Cocoa bindings into separate projects ([#1983](https://github.com/getsentry/sentry-dotnet/pull/1983))
  - NuGet package `Sentry` now depends on `Sentry.Bindings.Android` for `net6.0-android` targets.
  - NuGet package `Sentry` now depends on `Sentry.Bindings.Cocoa` for `net6.0-ios` and `net6.0-maccatalyst` targets.
- Exclude EF error message from logging ([#1980](https://github.com/getsentry/sentry-dotnet/pull/1980))
- Ensure logs with lower levels are captured by `Sentry.Extensions.Logging` ([#1992](https://github.com/getsentry/sentry-dotnet/pull/1992))
- Fix bug with pre-formatted strings passed to diagnostic loggers ([#2004](https://github.com/getsentry/sentry-dotnet/pull/2004))
- Fix DI issue by binding to MAUI using lifecycle events ([#2006](https://github.com/getsentry/sentry-dotnet/pull/2006))
- Unhide `SentryEvent.Exception` ([#2011](https://github.com/getsentry/sentry-dotnet/pull/2011))
- Bump `Google.Cloud.Functions.Hosting` to version 1.1.0 ([#2015](https://github.com/getsentry/sentry-dotnet/pull/2015))
- Fix default host issue for the Sentry Tunnel middleware ([#2019](https://github.com/getsentry/sentry-dotnet/pull/2019))

## 3.22.0

### Features

- `SentryOptions.AttachStackTrace` is now enabled by default. ([#1907](https://github.com/getsentry/sentry-dotnet/pull/1907))
- Update Sentry Android SDK to version 6.4.1 ([#1911](https://github.com/getsentry/sentry-dotnet/pull/1911))
- Update Sentry Cocoa SDK to version 7.24.1 ([#1912](https://github.com/getsentry/sentry-dotnet/pull/1912))
- Add `TransactionNameSource` annotation ([#1910](https://github.com/getsentry/sentry-dotnet/pull/1910))
- Use URL path in transaction names instead of "Unknown Route" ([#1919](https://github.com/getsentry/sentry-dotnet/pull/1919))
  - NOTE: This change effectively ungroups transactions that were previously grouped together under "Unkown Route".
- Add `User.Segment` property ([#1920](https://github.com/getsentry/sentry-dotnet/pull/1920))
- Add support for custom `JsonConverter`s ([#1934](https://github.com/getsentry/sentry-dotnet/pull/1934))
- Support more types for message template tags in SentryLogger ([#1945](https://github.com/getsentry/sentry-dotnet/pull/1945))
- Support Dynamic Sampling ([#1953](https://github.com/getsentry/sentry-dotnet/pull/1953))

### Fixes

- Reduce lock contention when sampling ([#1915](https://github.com/getsentry/sentry-dotnet/pull/1915))
- Dont send transaction for OPTIONS web request ([#1921](https://github.com/getsentry/sentry-dotnet/pull/1921))
- Fix missing details when aggregate exception is filtered out ([#1922](https://github.com/getsentry/sentry-dotnet/pull/1922))
- Exception filters should consider child exceptions of an `AggregateException` ([#1924](https://github.com/getsentry/sentry-dotnet/pull/1924))
- Add Blazor WASM detection to set IsGlobalModeEnabled to true ([#1931](https://github.com/getsentry/sentry-dotnet/pull/1931))
- Respect Transaction.IsSampled in SqlListener ([#1933](https://github.com/getsentry/sentry-dotnet/pull/1933))
- Ignore null Context values ([#1942](https://github.com/getsentry/sentry-dotnet/pull/1942))
- Tags should not differ based on current culture ([#1949](https://github.com/getsentry/sentry-dotnet/pull/1949))
- Always recalculate payload length ([#1957](https://github.com/getsentry/sentry-dotnet/pull/1957))
- Fix issues with envelope deserialization ([#1965](https://github.com/getsentry/sentry-dotnet/pull/1965))
- Set default trace status to `ok` instead of `unknown_error` ([#1970](https://github.com/getsentry/sentry-dotnet/pull/1970))
- Fix reported error count on a crashed session update ([#1972](https://github.com/getsentry/sentry-dotnet/pull/1972))

## 3.21.0

Includes Sentry.Maui Preview 3

### Features

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

Includes Sentry.Maui Preview 2

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

Includes Sentry.Maui Preview 1

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

- Ref moved SentryId from namespace Sentry.Protocol to Sentry (#643) @lucas-zimerman
- Ref renamed `CacheFlushTimeout` to `InitCacheFlushTimeout` (#638) @lucas-zimerman
- Add support for performance. ([#633](https://github.com/getsentry/sentry-dotnet/pull/633))
- Transaction (of type `string`) on Scope and Event now is called TransactionName. ([#633](https://github.com/getsentry/sentry-dotnet/pull/633))

## 3.0.0-alpha.6

- Abandon ValueTask #611
- Fix Cache deleted on HttpTransport exception. (#610) @lucas-zimerman
- Add `SentryScopeStateProcessor` #603
- Add net5.0 TFM to libraries #606
- Add more logging to CachingTransport #619
- Bump Microsoft.Bcl.AsyncInterfaces to 5.0.0 #618
- Bump `Microsoft.Bcl.AsyncInterfaces` to 5.0.0 #618
- `DefaultTags` moved from `SentryLoggingOptions` to `SentryOptions` (#637) @PureKrome
- `Sentry.Serilog` can accept DefaultTags (#637) @PureKrome

## 3.0.0-alpha.5

- Replaced `BaseScope` with `IScope`. (#590) @Tyrrrz
- Removed code coverage report from the test folder. (#592) @lucas-zimerman
- Add target framework NET5.0 on Sentry.csproj. Change the type of `Extra` where value parameter become nullable. @lucas-zimerman
- Implement envelope caching. (#576) @Tyrrrz
- Add a list of .NET Frameworks installed when available. (#531) @lucas-zimerman
- Parse Mono and IL2CPP stacktraces for Unity and Xamarin (#578) @bruno-garcia
- Update TFMs and dependency min version (#580) @bruno-garcia
- Run all tests on .NET 5 (#583) @bruno-garcia

## 3.0.0-alpha.4

- Add the client user ip if both SendDefaultPii and IsEnvironmentUser are set. (#1015) @lucas-zimerman
- Replace Task with ValueTask where possible. (#564) @Tyrrrz
- Add support for ASP.NET Core gRPC (#563) @Mitch528
- Push API docs to GitHub Pages GH Actions (#570) @bruno-garcia
- Refactor envelopes

## 3.0.0-alpha.3

- Add support for user feedback. (#559) @lucas-zimerman
- Add support for envelope deserialization (#558) @Tyrrrz
- Add package description and tags to Sentry.AspNet @Tyrrrz
- Fix internal url references for the new Sentry documentation. (#562) @lucas-zimerman

## 3.0.0-alpha.2

- Set the Environment setting to 'production' if none was provided. (#550) @PureKrome
- ASPNET.Core hosting environment is set to 'production' / 'development' (notice lower casing) if no custom options.Enviroment is set. (#554) @PureKrome
- Add most popular libraries to InAppExclude #555 (@bruno-garcia)
- Add support for individual rate limits.
- Extend `SentryOptions.BeforeBreadcrumb` signature to accept returning nullable values.
- Add support for envelope deserialization.

## 3.0.0-alpha.1

- Rename `LogEntry` to `SentryMessage`. Change type of `SentryEvent.Message` from `string` to `SentryMessage`.
- Change the type of `Gpu.VendorId` from `int` to `string`.
- Add support for envelopes.
- Publishing symbols package (snupkg) to nuget.org with sourcelink

## 3.0.0-alpha.0

- Move aspnet-classic integration to Sentry.AspNet (#528) @Tyrrrz
- Merge Sentry.Protocol into Sentry (#527) @Tyrrrz
- Framework and runtime info (#526) @bruno-garcia
- Add NRTS to Sentry.Extensions.Logging (#524) @Tyrrrz
- Add NRTs to Sentry.Serilog, Sentry.NLog, Sentry.Log4Net (#521) @Tyrrrz
- Add NRTs to Sentry.AspNetCore (#520) @Tyrrrz
- Fix CI build on GitHub Actions (#523) @Tyrrrz
- Add GitHubActionsTestLogger (#511) @Tyrrrz

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

- fix: MEL don't init if enabled (#460) @bruno-garcia
- feat: Device Calendar, Timezone, CultureInfo (#457) @bruno-garcia
- ref: Log out debug disabled (#459) @bruno-garcia
- dep: Bump PlatformAbstractions (#458) @bruno-garcia
- feat: Exception filter (#456) @bruno-garcia

## 2.1.5-beta

- fix: MEL don't init if enabled (#460) @bruno-garcia
- feat: Device Calendar, Timezone, CultureInfo (#457) @bruno-garcia
- ref: Log out debug disabled (#459) @bruno-garcia
- dep: Bump PlatformAbstractions (#458) @bruno-garcia
- feat: Exception filter (#456) @bruno-garcia

## 2.1.4

- NLog SentryTarget - NLogDiagnosticLogger for writing to NLog InternalLogger (#450) @snakefoot
- fix: SentryScopeManager dispose message (#449) @bruno-garcia
- fix: dont use Sentry namespace on sample (#447) @bruno-garcia
- Remove obsolete API from benchmarks (#445) @bruno-garcia
- build(deps): bump Microsoft.Extensions.Logging.Debug from 2.1.1 to 3.1.4 (#421) @dependabot-preview
- build(deps): bump Microsoft.AspNetCore.Diagnostics from 2.1.1 to 2.2.0 (#431) @dependabot-preview
- build(deps): bump Microsoft.CodeAnalysis.CSharp.Workspaces from 3.1.0 to 3.6.0 (#437) @dependabot-preview

## 2.1.3

- SentryScopeManager - Fixed clone of Stack so it does not reverse order (#420) @snakefoot
- build(deps): bump Serilog.AspNetCore from 2.1.1 to 3.2.0 (#411) @dependabot-preview
- Removed dependency on System.Collections.Immutable (#405) @snakefoot
- Fix Sentry.Microsoft.Logging Filter now drops also breadcrumbs (#440)

## 2.1.2-beta5

Fix Background worker dispose logs error message (#408)
Fix sentry serilog extension method collapsing (#406)
Fix Sentry.Samples.NLog so NLog.config is valid (#404)

Thanks @snakefoot and @JimHume for the fixes

Add MVC route data extraction to ScopeExtensions.Populate() (#401)

## 2.1.2-beta3

Fixed ASP.NET System.Web catch HttpException to prevent the request processor from being unable to submit #397 (#398)

## 2.1.2-beta2

- Ignore WCF error and capture (#391)

### 2.1.2-beta

- Serilog Sentry sink does not load all options from IConfiguration (#380)
- UnhandledException sets Handled=false (#382)

## 2.1.1

Bug fix:  Don't overwrite server name set via configuration with machine name on ASP.NET Core #372

## 2.1.0

- Set score url to fully constructed url #367 Thanks @christopher-taormina-zocdoc
- Don't dedupe from inner exception #363 - Note this might change groupings. It's opt-in.
- Expose FlushAsync to intellisense #362
- Protocol monorepo #325 - new protocol version whenever there's a new SDK release

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

- SentryTarget - GetTagsFromLogEvent with null check (#326)
- handled process corrupted (#328)
- sourcelink GA (#330)
- Adds ability to specify user values via NLog configuration (#336)
- Add option to ASP.NET Core to flush events after response complete (#288)
- Fixed race on `BackgroundWorker`  (#293)
- Exclude `Sentry.` frames from InApp (#272)
- NLog SentryTarget with less overhead for breadcrumb (#273)
- Logging on body not extracted (#246)
- Add support to DefaultTags for ASP.NET Core and M.E.Logging (#268)
- Don't use ValueTuple (#263)
- All public members were documented: #252
- Use EnableBuffering to keep request payload around: #250
- Serilog default levels: #237
- Removed dev dependency from external dependencies 4d92ab0
- Use new `Sentry.Protocol` 836fb07e
- Use new `Sentry.PlatformAbsrtractions` #226
- Debug logging for ASP.NET Classic #209
- Reading request body throws on ASP.NET Core 3 (#324)
- NLog: null check contextProp.Value during IncludeEventDataOnBreadcrumbs (#323)
- JsonSerializerSettings - ReferenceLoopHandling.Ignore (#312)
- Fixed error when reading request body affects collecting other request data (#299)
- `Microsoft.Extensions.Logging` `ConfigureScope` invocation. #208, #210, #224 Thanks @dbraillon
- `Sentry.Serilog` Verbose level. #213, #217. Thanks @kanadaj
- AppDomain.ProcessExit will close the SDK: #242
- Adds PublicApiAnalyzers to public projects: #234
- NLog: Utilizes Flush functionality in NLog target: #228
- NLog: Set the logger via the log event info in SentryTarget.Write, #227
- Multi-target .NET Core 3.0 (#308)

Major version bumped due to these breaking changes:

1. `Sentry.Protocol` version 2.0.0
   - Remove StackTrace from SentryEvent [#38](https://github.com/getsentry/sentry-dotnet-protocol/pull/38) - StackTrace is  either part of Thread or SentryException.
2. Removed `ContextLine` #223
3. Use `StackTrace` from `Threads` #222
4. `FlushAsync` added to `ISentryClient` #214

## 2.0.0-beta8

- SentryTarget - GetTagsFromLogEvent with null check (#326)
- handled process corrupted (#328)
- sourcelink GA (#330)
- Adds ability to specify user values via NLog configuration (#336)

## 2.0.0-beta7

Fixes:

- Reading request body throws on ASP.NET Core 3 (#324)
- NLog: null check contextProp.Value during IncludeEventDataOnBreadcrumbs (#323)
- JsonSerializerSettings - ReferenceLoopHandling.Ignore (#312)

Features:

- Multi-target .NET Core 3.0 (#308)

## 2.0.0-beta6

- Fixed error when reading request body affects collecting other request data (#299)

## 2.0.0-beta5

- Add option to ASP.NET Core to flush events after response complete (#288)
- Fixed race on `BackgroundWorker`  (#293)
- Exclude `Sentry.` frames from InApp (#272)
- NLog SentryTarget with less overhead for breadcrumb (#273)

## 2.0.0-beta4

- Logging on body not extracted (#246)
- Add support to DefaultTags for ASP.NET Core and M.E.Logging (#268)
- Don't use ValueTuple (#263)

## 2.0.0-beta3

- All public members were documented: #252
- Use EnableBuffering to keep request payload around: #250
- Serilog default levels: #237

Thanks @josh-degraw for:

- AppDomain.ProcessExit will close the SDK: #242
- Adds PublicApiAnalyzers to public projects: #234
- NLog: Utilizes Flush functionality in NLog target: #228
- NLog: Set the logger via the log event info in SentryTarget.Write, #227

## 2.0.0-beta2

- Removed dev dependency from external dependencies 4d92ab0
- Use new `Sentry.Protocol` 836fb07e
- Use new `Sentry.PlatformAbsrtractions` #226

## 2.0.0-beta

Major version bumped due to these breaking changes:

1. `Sentry.Protocol` version 2.0.0
   - Remove StackTrace from SentryEvent [#38](https://github.com/getsentry/sentry-dotnet-protocol/pull/38) - StackTrace is either part of Thread or SentryException.
2. Removed `ContextLine` #223
3. Use `StackTrace` from `Threads` #222
4. `FlushAsync` added to `ISentryClient` #214

Other Features:

- Debug logging for ASP.NET Classic #209

Fixes:

- `Microsoft.Extensions.Logging` `ConfigureScope` invocation. #208, #210, #224 Thanks @dbraillon
- `Sentry.Serilog` Verbose level. #213, #217. Thanks @kanadaj

## 1.2.1-beta

Fixes and improvements to the NLog integration: #207 by @josh-degraw

## 1.2.0

### Features

- Optionally skip module registrations #202 - (Thanks @josh-degraw)
- First NLog integration release #188 (Thanks @josh-degraw)
- Extensible stack trace #184 (Thanks @pengweiqhca)
- MaxRequestSize for ASP.NET and ASP.NET Core #174
- InAppInclude #171
- Overload to AddSentry #163 by (Thanks @f1nzer)
- ASP.NET Core AddSentry has now ConfigureScope: #160

### Bug fixes

- Don't override user #199
- Read the hub to take latest Client: 8f4b5ba

## 1.1.3-beta4

Bug fix: Don't override user  #199

## 1.1.3-beta3

- First NLog integration release #188 (Thanks @josh-degraw)
- Extensible stack trace #184 (Thanks @pengweiqhca)

## 1.1.3-beta2

Feature:

- MaxRequestSize for ASP.NET and ASP.NET Core #174
- InAppInclude #171

Fix: Diagnostic log order: #173 by @scolestock

## 1.1.3-beta

Fixed:

- Read the hub to take latest Client: 8f4b5ba1a3
- Uses Sentry.Protocol 1.0.4 4035e25

Feature

- Overload to `AddSentry` #163 by @F1nZeR
- ASP.NET Core `AddSentry` has now `ConfigureScope`: #160

## 1.1.2

Using [new version of the protocol with fixes and features](https://github.com/getsentry/sentry-dotnet-protocol/releases/tag/1.0.3).

Fixed:

ASP.NET Core integration issue when containers are built on the ServiceCollection after SDK is initialized (#157, #103 )

## 1.1.2-beta

Fixed:

- ASP.NET Core integration issue when containers are built on the ServiceCollection after SDK is initialized (#157, #103 )

## 1.1.1

Fixed:

- Serilog bug that self log would recurse #156

Feature:

- log4net environment via xml configuration #150 (Thanks Sébastien Pierre)

## 1.1.0

Includes all features and bug fixes of previous beta releases:

Features:

- Use log entry to improve grouping #125
- Use .NET Core SDK 2.1.401
- Make AddProcessors extension methods on Options public #115
- Format InternalsVisibleTo to avoid iOS issue: 94e28b3
- Serilog Integration #118, #145
- Capture methods return SentryId #139, #140
- MEL integration keeps properties as tags #146
- Sentry package Includes net461 target #135

Bug fixes:

- Disabled SDK throws on shutdown: #124
- Log4net only init if current hub is disabled #119

Thanks to our growing list of [contributors](https://github.com/getsentry/sentry-dotnet/graphs/contributors).

## 1.0.1-beta5

- Added `net461` target to Serilog package #148

## 1.0.1-beta4

- Serilog Integration #118, #145
- `Capture` methods return `SentryId` #139, #140
- MEL integration keeps properties as tags #146
- Revert reducing Json.NET requirements <https://github.com/getsentry/sentry-dotnet/commit/1aed4a5c76ead2f4d39f1c2979eda02d068bfacd>

Thanks to our growing [list of contributors](https://github.com/getsentry/sentry-dotnet/graphs/contributors).

## 1.0.1-beta3

Lowering Newtonsoft.Json requirements; #138

## 1.0.1-beta2

`Sentry` package Includes `net461` target #135

## 1.0.1-beta

Features:

- Use log entry to improve grouping #125
- Use .NET Core SDK 2.1.401
- Make `AddProcessors` extension methods on Options public  #115
- Format InternalsVisibleTo to avoid iOS issue: 94e28b3

Bug fixes:

- Disabled SDK throws on shutdown: #124
- Log4net only init if current hub is disabled #119

## 1.0.0

### First major release of the new .NET SDK

#### Main features

##### Sentry package

- Automatic Captures global unhandled exceptions (AppDomain)
- Scope management
- Duplicate events automatically dropped
- Events from the same exception automatically dropped
- Web proxy support
- HttpClient/HttpClientHandler configuration callback
- Compress request body
- Event sampling opt-in
- Event flooding protection (429 retry-after and internal bound queue)
- Release automatically set (AssemblyInformationalVersionAttribute, AssemblyVersion or env var)
- DSN discovered via environment variable
- Release (version) reported automatically
- CLS Compliant
- Strong named
- BeforeSend and BeforeBreadcrumb callbacks
- Event and Exception processors
- SourceLink (including PDB in nuget package)
- Device OS info sent
- Device Runtime info sent
- Enable SDK debug mode (opt-in)
- Attach stack trace for captured messages (opt-in)

##### Sentry.Extensions.Logging

- Includes all features from the `Sentry` package.
- BeginScope data added to Sentry scope, sent with events
- LogInformation or higher added as breadcrumb, sent with next events.
- LogError or higher automatically captures an event
- Minimal levels are configurable.

##### Sentry.AspNetCore

- Includes all features from the `Sentry` package.
- Includes all features from the `Sentry.Extensions.Logging` package.
- Easy ASP.NET Core integration, single line: `UseSentry`.
- Captures unhandled exceptions in the middleware pipeline
- Captures exceptions handled by the framework `UseExceptionHandler` and Error page display.
- Any event sent will include relevant application log messages
- RequestId as tag
- URL as tag
- Environment is automatically set (`IHostingEnvironment`)
- Request payload can be captured if opt-in
- Support for EventProcessors registered with DI
- Support for ExceptionProcessors registered with DI
- Captures logs from the request (using Microsoft.Extensions.Logging)
- Supports configuration system (e.g: appsettings.json)
- Server OS info sent
- Server Runtime info sent
- Request headers sent
- Request body compressed

All packages are:

- Strong named
- Tested on Windows, Linux and macOS
- Tested on .NET Core, .NET Framework and Mono

##### Learn more

- [Code samples](https://github.com/getsentry/sentry-dotnet/tree/master/samples)
- [Sentry docs](https://docs.sentry.io/quickstart/?platform=csharp)

Sample event using the log4net integration:
![Sample event in Sentry](https://github.com/getsentry/sentry-dotnet/blob/master/samples/Sentry.Samples.Log4Net/.assets/log4net-sample.gif?raw=true)

Download it directly from GitHub or using NuGet:

|      Integrations                 |        NuGet         |
| ----------------------------- | -------------------: |
|         **Sentry**            |    [![NuGet](https://img.shields.io/nuget/vpre/Sentry.svg)](https://www.nuget.org/packages/Sentry)   |
|     **Sentry.AspNetCore**     |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   |
| **Sentry.Extensions.Logging** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   |
| **Sentry.Log4Net** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net)   |

## 1.0.0-rc2

Features and improvements:

- `SentrySdk.LastEventId` to get scoped id
- `BeforeBreadcrumb` to allow dropping or modifying a breadcrumb
- Event processors on scope #58
- Event processor as `Func<SentryEvent,SentryEvent>`

Bug fixes:

- #97 Sentry environment takes precedence over ASP.NET Core

Download it directly below from GitHub or using NuGet:

|      Integrations                 |        NuGet         |
| ----------------------------- | -------------------: |
|         **Sentry**            |    [![NuGet](https://img.shields.io/nuget/vpre/Sentry.svg)](https://www.nuget.org/packages/Sentry)   |
|     **Sentry.AspNetCore**     |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   |
| **Sentry.Extensions.Logging** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   |
| **Sentry.Log4Net** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net)   |

## 1.0.0-rc

Features and improvements:

- Microsoft.Extensions.Logging (MEL) use framework configuration system #79 (Thanks @pengweiqhca)
- Use IOptions on Logging and ASP.NET Core integrations #81
- Send PII (personal identifier info, opt-in `SendDefaultPii`): #83
- When SDK is disabled SentryMiddleware passes through to next in pipeline: #84
- SDK diagnostic logging (option: `Debug`): #85
- Sending Stack trace for events without exception (like CaptureMessage, opt-in `AttachStackTrace`) #86

Bug fixes:

- MEL: Only call Init if DSN was provided <https://github.com/getsentry/sentry-dotnet/commit/097c6a9c6f4348d87282c92d9267879d90879e2a>
- Correct namespace for `AddSentry` <https://github.com/getsentry/sentry-dotnet/commit/2498ab4081f171dc78e7f74e4f1f781a557c5d4f>

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

## 0.0.1-preview5

Features:

- Support buffered gzip request #73
- Reduced dependencies from the ASP.NET Core integraiton
- InAppExclude configurable #75
- Duplicate event detects inner exceptions #76
- HttpClientHandler configuration callback #72
- Event sampling opt-in
- ASP.NET Core sends server name

Bug fixes:

- On-prem without chuncked support for gzip #71
- Exception.Data key is not string #77

**[Watch on youtube](https://www.youtube.com/watch?v=xK6a1goK_w0) how to use the ASP.NET Core integration**

Download it directly below from GitHub or using NuGet:

|      Integrations                 |        NuGet         |
| ----------------------------- | -------------------: |
|         **Sentry**            |    [![NuGet](https://img.shields.io/nuget/vpre/Sentry.svg)](https://www.nuget.org/packages/Sentry)   |
|     **Sentry.AspNetCore**     |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   |
| **Sentry.Extensions.Logging** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   |
| **Sentry.Log4Net** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Log4Net.svg)](https://www.nuget.org/packages/Sentry.Log4Net)   |

## 0.0.1-preview4

Features:

- Using [Sentry Protocol](https://github.com/getsentry/sentry-dotnet-protocol) as a dependency
- Environment can be set via `SentryOptions` #49
- Compress request body (configurable: Fastest, Optimal, Off) #63
- log4net integration
- SDK honors Sentry's 429 HTTP Status with Retry After header #61

Bug fixes:

- `Init` pushes the first scope #55, #54
- `Exception.Data` copied to `SentryEvent.Data` while storing the index of originating error.
- Demangling code ensures Function name available #64
- ASP.NET Core integration throws when Serilog added #65, #68, #67

Improvements to [the docs](https://getsentry.github.io/sentry-dotnet) like:

- Release discovery
- `ConfigureScope` clarifications
- Documenting samples

### [Watch on youtube](https://www.youtube.com/watch?v=xK6a1goK_w0) how to use the ASP.NET Core integration

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

- Filter duplicate events/exceptions #43
- EventProcessors can be added (sample [1](https://github.com/getsentry/sentry-dotnet/blob/dbb5a3af054d0ca6f801de37fb7db3632ca2c65a/samples/Sentry.Samples.Console.Customized/Program.cs#L151), [2](https://github.com/getsentry/sentry-dotnet/blob/dbb5a3af054d0ca6f801de37fb7db3632ca2c65a/samples/Sentry.Samples.Console.Customized/Program.cs#L41))
- ExceptionProcessors can be added #36 (sample [1](https://github.com/getsentry/sentry-dotnet/blob/dbb5a3af054d0ca6f801de37fb7db3632ca2c65a/samples/Sentry.Samples.Console.Customized/Program.cs#L172), [2](https://github.com/getsentry/sentry-dotnet/blob/dbb5a3af054d0ca6f801de37fb7db3632ca2c65a/samples/Sentry.Samples.Console.Customized/Program.cs#L42))
- Release is automatically discovered/reported #35
- Contexts is a dictionary - allows custom data #37
- ASP.NET integration reports context as server: server-os, server-runtime #37
- Assemblies strong named #41
- Scope exposes IReadOnly members instead of Immutables
- Released a [documentation site](https://getsentry.github.io/sentry-dotnet/)

Bug fixes:

- Strong name
- Logger provider gets disposed/flushes events

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

- Added `CaptureMessage`
- `BeforeSend` callback errors are sent as breadcrumbs
- `ASP.NET Core` integration doesn't add tags added by `Microsoft.Extensions.Logging`
- SDK name is reported depending on the package added
- Integrations API allows user-defined SDK integration
- Unhandled exception handler can be configured via integrations
- Filter kestrel log eventid 13 (application error) when already captured by the middleware

Bugs fixed:

- Fixed #28
- HTTP Proxy set to HTTP message handler

Download it directly from GitHub or using NuGet:

|      Integrations                 |        NuGet         |
| ----------------------------- | -------------------: |
|         **Sentry**            |    [![NuGet](https://img.shields.io/nuget/vpre/Sentry.svg)](https://www.nuget.org/packages/Sentry)   |
|     **Sentry.AspNetCore**     |   [![NuGet](https://img.shields.io/nuget/vpre/Sentry.AspNetCore.svg)](https://www.nuget.org/packages/Sentry.AspNetCore)   |
| **Sentry.Extensions.Logging** | [![NuGet](https://img.shields.io/nuget/vpre/Sentry.Extensions.Logging.svg)](https://www.nuget.org/packages/Sentry.Extensions.Logging)   |

## 0.0.1-preview1

Our first preview of the SDK:

Main features:

- Easy ASP.NET Core integration, single line: `UseSentry`.
- Captures unhandled exceptions in the middleware pipeline
- Captures exceptions handled by the framework `UseExceptionHandler` and Error page display.
- Captures process-wide unhandled exceptions (AppDomain)
- Captures logger.Error or logger.Critical
- When an event is sent, data from the current request augments the event.
- Sends information about the server running the app (OS, Runtime, etc)
- Informational logs written by the app or framework augment events sent to Sentry
- Optional include of the request body
- HTTP Proxy configuration

Also available via NuGet:

[Sentry](https://www.nuget.org/packages/Sentry/0.0.1-preview1)
[Sentry.AspNetCore](https://www.nuget.org/packages/Sentry.AspNetCore/0.0.1-preview1)
[Sentry.Extensions.Logging](https://www.nuget.org/packages/Sentry.Extensions.Logging/0.0.1-preview1)
