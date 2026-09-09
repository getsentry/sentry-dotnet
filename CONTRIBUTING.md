# Contributing

We love receiving PRs from the community with features and fixed.
For a big feature it's advised to raise an issue to discuss it first.

# Guidelines

* Please avoid mixing changes needed for a feature with other changes such as refactors, automated IDE changes like adding BOM characters, empty lines, etc.
* Feel free to start with a draft PR, while you work on it. You can ask pointed questions by reviewing your own code this way, while signaling to the reviewer that the PR isn't ready for review just yet.
* Mark the PR ready for review once you've completed the change, including:
  * The description should link to relevant context such as tickets, discussions or previous PRs. Consider screenshots of the events in Sentry or other relevant visual things.
  * Add tests that verify your change. The repo has lots of examples of Unit and integration tests. Including device tests that run on Android and iOS.
  * CI should be green.
  * The ideal state is where a reviewer approves and merges it immediately. But on more complex changes, some back and forth during reviews is expected.

## Code comments

We prefer code that explains itself, so comments should be kept to a minimum:

* When code needs explaining, prefer refactoring it (better variable and method names, clearer structure) over adding a comment.
* Where a comment is genuinely needed, keep it short and to the point.
* Detailed context belongs in the pull request description rather than inline. When a comment does need lengthy background, reference the relevant PR instead of inlining the explanation.

## TLDR

* Install the .NET SDKs
* Install PowerShell
* Install Xcode
* Check out the submodules: `git submodule update --init --recursive`
* Restore workloads with `dotnet workload restore` (needs `sudo` on a Mac)
* To quickly get up and running, you can just run `dotnet build SentryNoMobile.slnf` (you're skipping the mobile targets)
* To run a full build in Release mode and test, before pushing, run `./build.sh` or `./build.cmd`

## Minimal Dependencies

* The latest versions of the following .NET SDKs:
  - [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
  - [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
  - [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)

  *Technically, you only need the full SDK installation for the latest version. If you like, you can install the smaller ASP.NET Core Runtime packages for older versions. However, installing the full SDKs will also give you the runtimes.*

* [`pwsh`](https://github.com/PowerShell/PowerShell#get-powershell) Core version 6 or later on PATH.

* `CMake` on PATH. On Windows you can install the [C++ CMake tools for Windows](https://learn.microsoft.com/en-us/cpp/build/cmake-projects-in-visual-studio?view=msvc-170#installation). On macOS and Linux you can use your favourite package manager (e.g. `brew install cmake` or `apt install cmake`).

* On Windows:
  - [.NET Framework](https://dotnet.microsoft.com/download/dotnet-framework) 4.6.2 or higher.
  - `Sentry.DiagnosticSource.IntegrationTests.csproj` uses [SQL LocalDb](https://docs.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) - [download SQL LocalDB 2019](https://download.microsoft.com/download/7/c/1/7c14e92e-bdcb-4f89-b7cf-93543e7112d1/SqlLocalDB.msi). To avoid running these tests, unload `Sentry.DiagnosticSource.IntegrationTests.csproj` from the solution.
* On macOS/Linux
  - [Mono 6 or higher](https://www.mono-project.com/download/stable) to run the unit tests on the `net4x` targets.

## .NET MAUI Requirements

To build any of `Sentry.Maui`, `Sentry.Maui.Tests`, or `Sentry.Samples.Maui`, you'll need to have the MAUI workloads installed. You can do so by running `dotnet workload restore` from the root of the SDK's repository (or `sudo dotnet workload restore` on macOS/Linux).

See https://docs.microsoft.com/dotnet/maui/ for details. JetBrains also have a great blog post if you're developing on a Mac: https://blog.jetbrains.com/dotnet/2022/05/25/macos-environment-setup-for-maui-development/

Basically, if you can build and run the `MyMauiApp` example you should also be able to build and run the Sentry MAUI sample app.

### Targeting Android, iOS and Mac Catalyst

* Targeting the mobile platforms requires aditional dependencies. 
  - `Java` is required for building the Android bindings. If you're building Sentry using an IDE you provide the path to your Java installation via the IDE settings (open the settings for Visual Studio or Rider and search for "android"). Building Sentry from the command line (using `dotnet build`) requires `JAVA_HOME` to be available on the environment.
  - Compiling for iOS and Mac Catalyst happens on macOS only.

Although the files in `/src/Sentry/Platforms/` are part of the `Sentry` project, they are [conditionally targeted](https://github.com/getsentry/sentry-dotnet/blob/b1bfe1efc04eb4c911a85f1cf4cd2e5a176d7c8a/src/Sentry/Sentry.csproj#L19-L21) when the platform is Android, iOS, or Mac Catalyst. We build for Android on all platforms.

```xml
<!-- Platform-specific props included here -->
  <Import Project="Platforms\Android\Sentry.Android.props" Condition="'$(TargetPlatformIdentifier)' == 'android'" />
  <Import Project="Platforms\Cocoa\Sentry.Cocoa.props" Condition="'$(TargetPlatformIdentifier)' == 'ios' Or '$(TargetPlatformIdentifier)' == 'maccatalyst'" />
```

These `*.props` files are used to add platform-specific files, such as references to the binding projects for each native SDK. These binding projects are .NET wrappers around native Android or Cocoa SDK functions.

Also note `/Directory.Build.targets` contains some [convention-based rules](https://github.com/getsentry/sentry-dotnet/blob/4e7496b45465c5561767cfd8f2914740cc3dfdf6/Directory.Build.targets#L20-L37) to exclude code that is not relevant for the target platform. Developers using Visual Studio will need to enable `Show All Files` in order to be able to see these files, when working with the solution.

## Legacy ASP.NET solutions

When debugging a legacy ASP.NET application with project references to `Sentry.AspNet`, you may need the following workarounds to tooling issues:

#### Microsoft.WebApplication.targets not found

* [Disable Microsoft.WebApplication.targets in Rider](https://youtrack.jetbrains.com/issue/RIDER-87113/Cannot-build-.NET-Framework-projects-with-legacy-style-csproj-after-upgrading-to-2022.3.1)

#### CodeTaskFactory not supported

* [Disable CodeTaskFactory in Roslyn](https://github.com/aspnet/RoslynCodeDomProvider/issues/51#issuecomment-396329427)

## Solution Filters

_TLDR;_ when working with the Sentry codebase, you should use the solution filters (not the solutions).

_Full explanation:_ 

The `Sentry.slnx` solution contains all of the projects required to build Sentry, it's integrations and samples for every platform. However, the repository contains various solution filters that will be more practical for day-to-day tasks.

These solution filters get generated automatically by `/scripts/generate-solution-filters.ps1` so, although you can certainly create your own solution filters and manage these how you wish, don't try to modify any of the `*.slnf` files that are committed to source control. Instead, changes to these can be made by modifying `/scripts/generate-solution-filters-config.yml` and re-running the script that generates these.

Also note that script generates a `.generated.NoMobile.slnx` solution, which is an identical copy of `Sentry.slnx`. Again, we don't recommend opening this directly. It exists as a round about way to conditionally set build properties based on the solution name in certain solution filters. You should instead use those solution filters (e.g. `SentryNoMobile.slnf`) when working in the Sentry codebase.

## Git Hooks (Optional but Recommended)

To automatically check and fix code formatting before committing, you can set up a pre-commit hook:

```bash
./dev.cs setup-hooks
```

Before each commit, the hook runs `dotnet format` against your staged `.cs` files and auto-fixes any formatting issues. If fixes were applied, the commit is blocked — just stage the fixes and try again:

```bash
git add -u
git commit
```

Note: the hook skips automatically if you have unstaged changes, to avoid touching work in progress.

To opt out at any time:

```bash
./dev.cs remove-hooks
```

**Note:** You can also bypass the hook for a specific commit using `git commit --no-verify` if needed.

## API changes approval process

This repository uses [Verify](https://github.com/VerifyTests/Verify) to store the public API diffs in snapshot files. When a change involves modifying the public API area (by for example adding a public method), that change will need to be approved, otherwise the CI process will fail.

To do that, run the build locally (i.e: `./build.sh` or `build.cmd`) and commit the `verify` files that were changed.

## Changelog

We'd love for users to update the SDK everytime and as soon as we make a new release. But in reality most users rarely update the SDK.
To help users see value in updating the SDK, we maintain a changelog file with entries split between headings such as `### Features` and `### Fixes`.

**Do not edit `CHANGELOG.md` manually.** The changelog is generated automatically at release time by [craft](https://github.com/getsentry/craft) (`changelogPolicy: auto` in `.craft.yml`) from the pull requests merged since the previous release. Entries are categorized from your [commit message / PR title](https://develop.sentry.dev/engineering-practices/commit-messages/) (e.g. a `feat:` PR becomes a Feature, a `fix:` PR becomes a Fix), so there's nothing to add by hand.

If the PR title doesn't capture the change well — you want more detail, or a single PR should produce several entries — add a `### Changelog Entry` section to the PR description. craft uses the text under that heading verbatim instead of the PR title. See [Custom changelog entries from PR descriptions](https://craft.sentry.dev/configuration/#custom-changelog-entries-from-pr-descriptions).

If the change isn't user-facing and you'd rather it not appear in the changelog at all, add the `skip-changelog` label to the PR or write `#skip-changelog` in the PR description.

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

## Integration tests

Directory [./integration-test](./integration-test/) contains [Pester](https://pester.dev/)-based integration tests.
These tests create sample apps with `dotnet new` and run against local nuget packages (.nuget files).
In CI, these packages are expected to be present, while locally, scripts will run `nuget pack` automatically.

### Running integration tests locally

You can run individual tests either via Pester integration (e.g. in VS Code), or from command line: `./integration-test/cli.Tests.ps1`. Consult Pester docs for details on how to write tests.

Because these tests rely on a Sentry server mock (`Invoke-SentryServer`) from <https://github.com/getsentry/github-workflows/tree/main/sentry-cli/integration-test>, you need to check out [getsentry/github-workflows](https://github.com/getsentry/github-workflows) as a sibling directory next to your `getsentry/sentry-dotnet` checkout.

## Upgrading to a new .NET major version

Every year we take a dependency on a .NET preview, follow it through the RCs, and land on GA.
The same handful of things break each time, so this is the list of what to expect and where to
fix it. Most of these repeat annually — please extend this section rather than replacing it.

The single most useful habit: **`dotnet pack` and the integration tests, run locally, catch most
of this.** Building and testing a solution filter does not — several of these only appear at pack
time, in the integration tests, or in projects that aren't in any filter.

### Recurring, expect these every time

| Symptom | Where | What to do |
| --- | --- | --- |
| `NU5104` A stable release of a package should not have a prerelease dependency | `Directory.Build.props` | While we depend on preview `Microsoft.*` packages our own packages must be prerelease too. Set `VersionPrefix` to the upcoming major and `VersionSuffix` to `prerelease`. |
| `CONTAINER1015` Unable to access the repository `dotnet/runtime-deps` at tag ... | `.github/workflows/build.yml` | The SDK derives a `runtime-deps` tag from the runtime version and it doesn't exist yet for previews. Set `CONTAINER_BASE_IMAGE_GLIBC` / `CONTAINER_BASE_IMAGE_MUSL` at the top of the workflow. Which runtimes need it varies: .NET 10 needed only musl, .NET 11 needed glibc too (images moved from noble to resolute). |
| A new TFM silently gets **no** package references | any `'$(TargetFrameworkVersion)' == 'v{N}.0'` `ItemGroup` | These are exact-match, so a new TFM matches nothing and gets no packages — with no error. Add a `v{N+1}.0` sibling to every one of them. This is what caused the Android `XA4242` Java dependency failures in the .NET 11 bump. |
| `XA5207` Could not find `android.jar` for API level {N} | `.github/actions/environment/action.yml` | Add the new platform to the `setup-android` `packages:` list, e.g. `platforms;android-37.0`. Check the exact id — preview API levels carry a minor, `android-37.0` not `android-37`. |
| Test hosts fail to start: `You must install or update .NET` | `.github/actions/environment/action.yml` | Once `global.json` pins the new SDK, the previous runtime no longer comes with it, but we still target and test it. Add `{N-1}.0.x` to `dotnet-version`. Most runners have it preinstalled, so this usually only shows up on the Alpine containers. |
| `XA4216` deployment target not supported / `SupportedOSPlatformVersion` lower than minimum | `Directory.Build.props` **and** `Directory.Build.targets` | Platform minimums rise with each release. Condition the new value on the new TFM so existing consumers aren't affected — see the Android and MacCatalyst entries there. These are user-facing breaking changes; add them to the PR's `### Changelog Entry`. |
| `NETSDK1094` / `NETSDK1096` optimizing assemblies for performance failed | app `.csproj`s with an Android TFM | ReadyToRun can't target Android at all (`crossgen2`: `Target OS 'android' is not supported`), but the SDK may enable it by default. Set `PublishReadyToRun=false` on the affected app. |
| iOS app builds fail with `requires Xcode X, the current version is Y` | `integration-test/*.ps1`, device test apps | Each .NET for iOS SDK pack pins an **exact** Xcode version, so only one iOS TFM is buildable on a given machine. Build the TFM matching the Xcode that CI pins, and don't try to cover two. Libraries skip this check, which is why only app builds are affected. |
| Verify snapshots missing for the new TFM | `test/**/*.verified.txt` | Delete the snapshots for dropped TFMs. New ones for macOS-covered TFMs regenerate on a local test run. The `.Windows.` and `Net4_8` ones can only be produced on Windows — take them from the `<rid>-verify-test-results` CI artifact rather than hand-writing them; they are UTF-8 with BOM and no trailing newline. |

### Places that don't follow the usual rules

- **`samples/`** deliberately uses literal TFMs, not the centralized properties, because samples are
  documentation people copy. Bump them by hand.
- **`test/Sentry.TrimTest`, `test/Sentry.MauiTrimTest` and `test/AndroidTestApp`** ship empty
  `Directory.Build.props`/`.targets` stubs to isolate themselves from the repo's build
  customization. The centralized TFM properties are unavailable there — keep their TFMs literal.
  Using a property yields an empty `TargetFrameworks` and a confusing
  `MSB4006: circular dependency ... _GetRequiredWorkloads`.
- **`Sentry-CI-Build-macOS.slnf` is not every project.** A green build of it still leaves the
  Playwright test apps, `Sentry.AspNet.Tests`, `AndroidTestApp`, both trim tests and the
  integration-test app unbuilt. Build those separately.

### Verifying locally before pushing

Run these **serially** — `scripts/build-sentry-native.ps1 -Clean` wipes the CMake cache, so a
concurrent build in the same worktree fails with a misleading `CMAKE_C_COMPILER not set`.

```shell-script
dotnet build Sentry-CI-Build-macOS.slnf -c Release
dotnet pack  Sentry-CI-Build-macOS.slnf -c Release --no-build   # catches NU5104 / NU5026
# the projects outside the filter, at the TFMs CI builds
dotnet build test/Sentry.MauiTrimTest/Sentry.MauiTrimTest.csproj -c Release -f net10.0-android36.0
# ...and the integration tests
pwsh -c "Invoke-Pester integration-test/aot.Tests.ps1, integration-test/cli.Tests.ps1"
```

What genuinely can't be checked locally on macOS: the Windows jobs (`net48`, the `.Windows.` and
`Net4_8` snapshots), the Linux-only `Container` test in `aot.Tests.ps1` (it requires a Linux
*host*, not just Docker), and anything caused by the CI runner's environment rather than the code —
for those, read `.github/actions/environment/action.yml` against the TFMs you just added.

## Maintaining the Ben.Demystifier Submodule

This repo uses a variety of techniques to vendor in third party code without creating external dependencies. One of
those is submodules.

One of those submodules is Ben.Demystifier, which was originally written by Ben Adams. Attempts to contact Ben in recent 
years have been unsuccessful, so we've started maintaining a permanent fork of the project at:
- https://github.com/getsentry/Ben.Demystifier

Any significant changes to the submodule should be made in a branch and merged into the submodule's `main` branch. 
However, many of the Ben.Demystifier members are public. That makes sense if people are using Ben.Demystifier as a 
library, but in this repo we want to keep those members internal.

Once changes to Ben.Demystifier have been merged into the main branch then, the `internal` branch of Ben.Demystifier 
should be updated from the main branch and the `modules/make-internal.sh` script run again (if necessary). This repo 
should reference the most recent commit on the `internal` branch of Ben.Demystifier then (functionally identical to the
main branch - the only difference being the changes to member visibility).

## Sentry Cocoa SDK checkout

`Sentry.Bindings.Cocoa` always builds the Sentry Cocoa SDK from source, from the
[getsentry/sentry-cocoa](https://github.com/getsentry/sentry-cocoa/) submodule at
`modules/sentry-cocoa` (`scripts/build-sentry-cocoa.sh`, invoked automatically by
the build). Pre-built release artifacts can't be used: the `SentryObjC` hybrid-API
frameworks are only published as self-contained bundles that would embed a second
copy of the SDK alongside `Sentry.framework` (see
[#5331](https://github.com/getsentry/sentry-dotnet/issues/5331)).

To build against a different Cocoa SDK version, check out the desired ref in the
submodule **and stage it** — the solution build automatically runs
`git submodule update` (see `before.Sentry.sln.targets`), which reverts the
submodule to the pinned commit unless the index already records your ref:

```sh
$ git -C modules/sentry-cocoa fetch origin
$ git -C modules/sentry-cocoa checkout <tag-or-sha>
$ git add modules/sentry-cocoa   # otherwise the build restores the pinned commit
$ dotnet build ...               # rebuilds the Cocoa SDK from the new ref
```

## Local Sentry Android SDK checkout

Similarly, by default, `Sentry.Bindings.Android` downloads a pre-built Sentry Android SDK from
Maven. The version is specified in the `SentryAndroidSdkVersion` build property in `Sentry.Bindings.Android.csproj`.

If you want to build an unreleased Sentry Android SDK version from source instead,
you'll need to clone both the sentry-java and the sentry-native repositories and publish these locally:
```sh
$ cd $(LocalSentryJavaRepoDir) && ./gradlew publishToMavenLocal
$ cd $(LocalSentryNativeRepoDir)/ndk && ./gradlew publishToMavenLocal
```

You'll also need to set `<UseLocalSentryMavenRepo>true</UseLocalSentryMavenRepo>` and `<SentryNativeNdkVersion>{whatever_version_you_checked_out}</SentryNativeNdkVersion>`
in the `Sentry.Bindings.Android.csproj`file.

To switch back again, simply revert those two build properties to their original values.

## AI Workflows

### AGENTS.md

We guide coding agents via the [AGENTS.md](./AGENTS.md) file.
See also https://agents.md/.

And yes, Sentry has a [Skill](https://github.com/getsentry/skills) to maintain the `AGENTS.md` file.

### .agents

We use [dotagents](https://github.com/getsentry/dotagents) as a package manager for agent skills and more.
See [agents.toml](./agents.toml) for our current configuration.

### Warden

We use [Warden](https://github.com/getsentry/warden) as a tool to run _Agent Skills_ against code changes, both locally and in CI.
See [warden.toml](./warden.toml) for our current configuration.
