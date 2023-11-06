# Contributing

We love receiving PRs from the community with features and fixed.
For big feature it's advised to raise an issue to discuss it first.

## TLDR

* Install the .NET SDKs
* Install PowerShell
* Restore workloads with `dotnet workload restore` (needs `sudo` on a Mac)
* To quickly get up and running, you can just run `dotnet build Sentry.sln`
* To run a full build in Release mode and test, before pushing, run `./build.sh` or `./build.cmd`

## Dependencies

* The latest versions of the following .NET SDKs:
  - [.NET 7.0](https://dotnet.microsoft.com/download/dotnet/7.0)
  - [.NET 6.0](https://dotnet.microsoft.com/download/dotnet/6.0)

  *Technically, you only need the full SDK install for the latest version (7.0).  If you like, you can install the smaller ASP.NET Core Runtime packages for .NET 6.0 .  However, installing the full SDKs will also give you the runtimes.*

  *If using an M1 ("Apple silicon") processor, read [the special instructions below](#special-instructions-for-apple-silicon-cpus).*

* On Windows: 
  - [.NET Framework](https://dotnet.microsoft.com/download/dotnet-framework) 4.6.2 or higher.
  - [C++ CMake tools for Windows](https://learn.microsoft.com/en-us/cpp/build/cmake-projects-in-visual-studio?view=msvc-170#installation)
  - `Sentry.DiagnosticSource.IntegrationTests.csproj` uses [SQL LocalDb](https://docs.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) - [download SQL LocalDB 2019](https://download.microsoft.com/download/7/c/1/7c14e92e-bdcb-4f89-b7cf-93543e7112d1/SqlLocalDB.msi). To avoid running these tests, unload `Sentry.DiagnosticSource.IntegrationTests.csproj` from the solution.
* On macOS/Linux
  - [Mono 6 or higher](https://www.mono-project.com/download/stable) to run the unit tests on the `net4x` targets.
  - Install `CMake` using your favourite package manager (e.g. `brew install cmake`) 

## .NET MAUI Requirements

To build any of `Sentry.Maui`, `Sentry.Maui.Tests`, or `Sentry.Samples.Maui`, you'll need to have .NET SDK 7.0.400 or greater installed, and have installed the MAUI workloads installed, either through Visual Studio setup, or by running `dotnet workload restore` (or `dotnet workload install maui`) from the Sentry source code root directory.
You may also need other platform dependencies.  

See https://docs.microsoft.com/dotnet/maui/ for details. JetBrains also have a great blog post if you're developing on a Mac: https://blog.jetbrains.com/dotnet/2022/05/25/macos-environment-setup-for-maui-development/

Basically, if you can build and run the "MyMauiApp" example you should also be able to build and run the Sentry MAUI sample app.

### Targeting Android, iOS and Mac Catalyst

Although the files in `/src/Sentry/Platforms/` are part of the `Sentry` project, they are [conditionally targeted](https://github.com/getsentry/sentry-dotnet/blob/b1bfe1efc04eb4c911a85f1cf4cd2e5a176d7c8a/src/Sentry/Sentry.csproj#L19-L21) when the platform is Android, iOS or Mac Catalyst.  We build for Android on all platforms, but currently compile iOS and Mac Catalyst _only when building on a Mac_.

```xml
<!-- Platform-specific props included here -->
  <Import Project="Platforms\Android\Sentry.Android.props" Condition="'$(TargetPlatformIdentifier)' == 'android'" />
  <Import Project="Platforms\iOS\Sentry.iOS.props" Condition="'$(TargetPlatformIdentifier)' == 'ios' Or '$(TargetPlatformIdentifier)' == 'maccatalyst'" />
```

These `*.props` files are used to add platform-specific files, such as references to the binding projects for each native SDK (which provide .NET wrappers around native Android or Cocoa functions).

Also note `/Directory.Build.targets` contains some [convention based rules](https://github.com/getsentry/sentry-dotnet/blob/b1bfe1efc04eb4c911a85f1cf4cd2e5a176d7c8a/Directory.Build.targets#L17-L35) to exclude code that is not relevant for the target platform. Developers using Visual Studio will need to enable `Show All Files` in order to be able to see these files, when working with the solution. 

## Solution Filters

Most contributors will rarely need to load Sentry.sln. The repository contains various solution filters that will be more practical for day to day tasks. 

These solution filters get generated automatically by `/scripts/generate-solution-filters.ps1` so, although you can certainly create your own solution filters and manage these how you wish, don't try to modify any of the `*.slnf` files that are committed to source control. Instead, changes to these can be made by modifying `/scripts/generate-solution-filters-config.yml` and re-running the script that generates these.

## API changes approval process

This repository uses [Verify](https://github.com/VerifyTests/Verify) to store the public API diffs in snapshot files.
When a change involves modifying the public API area (by for example adding a public method),
that change will need to be approved, otherwise the CI process will fail.

To do that, run the build locally (i.e: `./build.sh` or `build.cmd`)
and commit the `verify` files that were changed.


## Special Instructions for Apple Silicon CPUs

Apple Silicon processors (such as the "M1") are arm64 processosr. While .NET 6 and higher run natively on this arm64 under macOS, previous versions are only built for x64. To get everything working correctly take the following steps:

- Always install the arm64 release of .NET 6 and 7, through the normal process described above.

If you are only running `dotnet test Sentry.sln` on the command line, you don't need to do anything else.

If you are using JetBrains Rider as your IDE, you should install the arm64 version of Rider.  Within Rider, the .NET SDK used for build and tests is selected under `Preferences` -> `Build, Execution, Deployment` -> `Toolset and Build`.

When the .NET Core CLI executable path is set to `/usr/local/share/dotnet/dotnet`, that's an arm64 version of the .NET SDK.
- This should be your usual default setting.
- You will be able to build for all versions of .NET installed, both arm64 and x64.
- However, you will only be able to debug and run unit tests in Rider using the arm64 versions of the .NET runtimes you have installed.

When the .NET Core CLI executable path is set to `/usr/local/share/dotnet/x64/dotnet`, that's an x64 version of the .NET SDK.
- .NET Core 3.1 and older are only 64 bits but are **no longer supoported by this repository** as of version 4.0.0 of the SDK. You shouldn't need to have x64 version installed to contribute.
- Keep in mind that x64 is always slower and consumes more battery, as it runs through emulation.

Note that the MSBuild version should always be `17.0` but will change paths based on whether you have selected an arm64 or x64 SDK.  If Rider auto-detects an older MSBuild, change it manually to 17.0.

## Changelog

We'd love for users to update the SDK everytime and as soon as we make a new release. But in reality most users rarely update the SDK.
To help users see value in updating the SDK, we maintain a changelog file with entries split between two headings:

1. `### Features`
2. `### Fixes`

We add the heading in the first PR that's adding either a feature or fixes in the current release.
After a release, the [changelog file will contain only the last release entries](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md).

When you open a PR in such case, you need to add a heading 2 named `## Unreleased`, which is replaced during release with the version number chosen.
Below that, you'll add the heading 3 mentioned above. For example, if you're adding a feature "Attach screenshots when capturing errors on WPF", right after a release, you'd add to the changelog:

```
## Unreleased

### Features

* Attach screenshots when capturing errors on WPF (#PR number)
```

There's a GitHub action check to verify if an entry was added. If the entry isn't a user-facing change, you can skip the verification with `#skip-changelog` written to the PR description. The bot writes a comment in the PR with a suggestion entry to the changelog based on the PR title.

## Naming tests

Ideally we like tests to be named following the convention `Method_Context_Expectation`.

[For example](https://github.com/getsentry/sentry-dotnet/blob/ebd70ffafd5f8bd5eb6bb9ee1a03cac77ae67b8d/test/Sentry.Tests/HubTests.cs#L43C1-L44C68):
```csharp
    [Fact]
    public void PushScope_BreadcrumbWithinScope_NotVisibleOutside()
```

## Verify tests

Some tests use [Verify](https://github.com/VerifyTests/Verify) to check returned objects against snapshots that are part of the repo.
In case you're making code changes that produce many (intended) changes in those snapshots, you can use [accept-verifier-changes.ps1](./scripts/accept-verifier-changes.ps1) like this:

```shell-script
dotnet test
pwsh ./scripts/accept-verifier-changes.ps1
```

You may need to run this multiple times because `dotnet test` stops after a certain number of failures.
