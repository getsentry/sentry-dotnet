# Contributing

We love receiving PRs from the community with features and fixed. 
For big feature it's advised to raise an issue to discuss it first.

## TLDR:

* Install the .NET SDKs
* To quickly get up and running, you can just run `dotnet build`
* To run a full build and test locally before pushing, run `./build.sh` or `./buld.ps1`

## Dependencies

* The latest versions of the following .NET SDKs:
  - [.NET 6.0](https://dotnet.microsoft.com/download/dotnet/6.0)
  - [.NET 5.0](https://dotnet.microsoft.com/download/dotnet/5.0)
  - [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet/3.1)
  - [.NET Core 2.1](https://dotnet.microsoft.com/download/dotnet/2.1)

  *If using an M1 ("Apple silicon") processor, read [the special instructions below](#special-instructions-for-apple-m1-cpus).*

* On Windows: [.NET Framework](https://dotnet.microsoft.com/download/dotnet-framework) 4.6.2 or higher.
  - `Sentry.DiagnosticSource.IntegrationTests.csproj` uses [SQL LocalDb](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) - [download SQL LocalDB 2019](https://download.microsoft.com/download/7/c/1/7c14e92e-bdcb-4f89-b7cf-93543e7112d1/SqlLocalDB.msi). To avoid running these tests, unload `Sentry.DiagnosticSource.IntegrationTests.csproj` from the solution.
* On macOS/Linux: [Mono 6 or higher](https://www.mono-project.com/download/stable) to run the unit tests on the `net4x` targets.

## API changes approval process

This repository uses [Verify](https://github.com/VerifyTests/Verify) to store the public API diffs in snapshot files.
When a change involves modifying the public API area (by for example adding a public method),
that change will need to be approved, otherwise the CI process will fail.

To do that, run the build locally (i.e: `./build.sh` or `pwsh .\build.ps1`)
and commit the `verify` files that were changed.


## Special Instructions for Apple M1 CPUs

The M1 ("Apple silicon") is an Arm64 processor. While .NET 6 runs natively on this architecture under macOS, .NET 5 and previous versions of the SDK are only built for x64. To get everything working correctly take the following steps:

- Install the `Arm64` release of the latest .NET 6 SDK through the normal process.
- Also install the `x64` release of the latest .NET 6 SDK through the normal process.
  - If prompted, you will need to allow Apple's [Rosetta](https://support.apple.com/HT211861) to be installed.  If you have previously done this for another app, you won't be prompted again.
- Install the latest .NET 5 SDK through the normal process.  It is only available for `x64`.
- Install the latest .NET Core 3.1 SDK through the normal process.  It is only available for `x64`.
- Install the .NET Core 2.1 SDK through a custom process as described below, which forces the SDK to be installed to a usable location.  
  *.NET Core 2.1 is no longer supported by Microsoft and is only used in our unit tests.  You can skip this step if you don't care about tests failing on .NET Core 2.1.*
  - Download the [dotnet-install bash script](https://dot.net/v1/dotnet-install.sh) from Microsoft.
  - Run the following commands:
    ```sh
    chmod +x ./dotnet-install.sh
    sudo ./dotnet-install.sh --version 2.1.818 --arch x64 --install-dir /usr/local/share/dotnet/x64
     ```
- If using JetBrains Rider, use it to launch `Sentry.sln` (or one of the `slnf` files).  Then go to `Preferences` -> `Build, Execution, Deployment` -> `Toolset and Build`, and set the following, which will tell Rider to use the x64 versions of the SDKs.  (This is *required* to run unit tests or debug using .NET 5.0 and earlier .NET Core SDKs.)
  - .NET Core CLI executable path: `/usr/local/share/dotnet/x64/dotnet`
  - Use MSBuild version: `17.0 - /usr/local/share/dotnet/x64/sdk/6.0.201/MSBuild.dll` (or higher version)
  - Under `MSBuild global properties` add a new item with name `_EnableMacOSCodeSign` and value `false`.  (See [dotnet/sdk#2201](https://github.com/dotnet/sdk/issues/22201#issuecomment-1089129133))
  - Click the arrow next to the "Save" button and choose `Solution "Sentry" personal"` to save these settings locally for yourself only.

Note that if you have accidentally corrupted your .NET installation by trying to install the .NET Core 2.1 SDK to the default location, you can wipe clean with the following, then start over:

```sh
sudo rm -r /usr/local/share/dotnet
sudo rm -r /etc/dotnet
```
