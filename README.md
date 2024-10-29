# Device test errors on net8.0-ios

This repo reproduces an issue when running the sentry-dotnet device tests against a net8.0-ios target. 

## Prerequisites

- Xcode 16
- iOS 18.0 Simulator
- .NET 8 or higher installed (I'm running net9.0-rc2)
- xharness 10.0.0-*

## Running the tests

Run the included powershell script, passing in the target framework as a parameter:

### net7.0:
```
pwsh scripts/device-test.ps1 ios -tfm net7.0
```

### net8.0:
```
pwsh scripts/device-test.ps1 ios -tfm net7.0
```

## Expected results

The tests should pass regardless of whether `net7.0-ios` or `net8.0-ios` is being targeted. 

## Actual results

The tests pass when run against `net7.0-ios`. However against `net8.0-ios` instead a `System.PlatformNotSupportedException` is thrown by `Castle.DynamicProxy.ModuleScope.CreateModule` (see test results in the `/test_output` folder once the tests have run, for a full stack trace).