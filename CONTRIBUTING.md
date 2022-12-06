# Contributing

We love receiving PRs from the community with features and fixed.
For big feature it's advised to raise an issue to discuss it first.

## TLDR

* Install the .NET SDKs
* To quickly get up and running, you can just run `dotnet build`
* To run a full build and test locally before pushing, run `./build.sh` or `./buld.ps1`

## Dependencies

* The latest versions of the following .NET SDKs:
  - [.NET 7.0](https://dotnet.microsoft.com/download/dotnet/7.0)
  - [.NET 6.0](https://dotnet.microsoft.com/download/dotnet/6.0)
  - [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet/3.1)

  *Technically, you only need the full SDK install for the latest version (7.0).  If you like, you can install the smaller ASP.NET Core Runtime packages for .NET 6.0 and .NET Core 3.1.  However, installing the full SDKs will also give you the runtimes.*

  *If using an M1 ("Apple silicon") processor, read [the special instructions below](#special-instructions-for-apple-silicon-cpus).*

* On Windows: [.NET Framework](https://dotnet.microsoft.com/download/dotnet-framework) 4.6.2 or higher.
  - `Sentry.DiagnosticSource.IntegrationTests.csproj` uses [SQL LocalDb](https://docs.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) - [download SQL LocalDB 2019](https://download.microsoft.com/download/7/c/1/7c14e92e-bdcb-4f89-b7cf-93543e7112d1/SqlLocalDB.msi). To avoid running these tests, unload `Sentry.DiagnosticSource.IntegrationTests.csproj` from the solution.
* On macOS/Linux: [Mono 6 or higher](https://www.mono-project.com/download/stable) to run the unit tests on the `net4x` targets.

## .NET MAUI Requirements

To build any of `Sentry.Maui`, `Sentry.Maui.Tests`, or `Sentry.Samples.Maui`, you'll need to have .NET SDK 6.0.400 or greater installed, and have installed the MAUI workloads installed, either through Visual Studio setup, or by running `dotnet workload restore` (or `dotnet workload install maui`) from the Sentry source code root directory.
You may also need other platform dependencies.  See https://docs.microsoft.com/dotnet/maui/ for details.  Basically, if you can build and run the "MyMauiApp" example you should also be able to build and run the Sentry MAUI sample app.

## API changes approval process

This repository uses [Verify](https://github.com/VerifyTests/Verify) to store the public API diffs in snapshot files.
When a change involves modifying the public API area (by for example adding a public method),
that change will need to be approved, otherwise the CI process will fail.

To do that, run the build locally (i.e: `./build.sh` or `pwsh .\build.ps1`)
and commit the `verify` files that were changed.


## Special Instructions for Apple Silicon CPUs

Apple Silicon processors (such as the "M1") are arm64 processosr. While .NET 6 and higher run natively on this arm64 under macOS, previous versions are only built for x64. To get everything working correctly take the following steps:

- Always install the arm64 release of .NET 6 and 7, through the normal process described above.
- Always install the x64 release of .NET Core 3.1 (it is not avaialable for arm64).
  - If prompted, you will need to allow Apple's [Rosetta](https://support.apple.com/HT211861) to be installed.  If you have previously done this for another app, you won't be prompted again.

If you are only running `dotnet test` on the command line, you don't need to do anything else.

If you are using JetBrains Rider as your IDE, you should install the arm64 version of Rider.  Within Rider, the .NET SDK used for build and tests is selected under `Preferences` -> `Build, Execution, Deployment` -> `Toolset and Build`.

When the .NET Core CLI executable path is set to `/usr/local/share/dotnet/dotnet`, that's an arm64 version of the .NET SDK.
- This should be your usual default setting.
- You will be able to build for all versions of .NET installed, both arm64 and x64.
- However, you will only be able to debug and run unit tests in Rider using the arm64 versions of the .NET runtimes you have installed.

When the .NET Core CLI executable path is set to `/usr/local/share/dotnet/x64/dotnet`, that's an x64 version of the .NET SDK.
- You will need to switch to this if you need to debug or run unit tests for .NET Core 3.1.
- Only targets which you have x64 SDKs will work when this is selected.  Thus, you *might* want to also install x64 versions of the newer .NET 6 and 7 SDKs, which would allow you to run tests for all target frameworks together.
- Keep in mind that x64 is always slower and consumes more battery, as it runs through emulation.  Thus you should use this mode only when needed.

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

## Verify tests

Some tests use [Verify](https://github.com/VerifyTests/Verify) to check returned objects against snapshots that are part of the repo.
In case you're making code changes that produce many (intended) changes in those snapshots, you can use [accept-verifier-changes.ps1](./scripts/accept-verifier-changes.ps1) like this:

```shell-script
dotnet test
pwsh ./scripts/accept-verifier-changes.ps1
```

You may need to run this multiple times because `dotnet test` stops after a certain number of failures.