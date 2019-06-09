<p align="center">
  <a href="https://sentry.io" target="_blank" align="center">
    <img src="https://sentry-brand.storage.googleapis.com/sentry-logo-black.png" width="280">
  </a>
  <br />
</p>


Sentry.PlatformAbstractions
===========
[![Travis](https://travis-ci.org/getsentry/sentry-dotnet-platform-abstractions.svg?branch=master)](https://travis-ci.org/getsentry/sentry-dotnet-platform-abstractions)
[![AppVeyor](https://ci.appveyor.com/api/projects/status/arv807179rg9sg1r/branch/master?svg=true)](https://ci.appveyor.com/project/sentry/sentry-dotnet-platform-abstractions/branch/master)


|      Package name                 |        NuGet         |
| ----------------------------- | -------------------: |
|         **Sentry.Protocol**            |    [![NuGet](https://img.shields.io/nuget/vpre/Sentry.PlatformAbstractions.svg)](https://www.nuget.org/packages/Sentry.PlatformAbstractions)   |

The goal of this package is to simplify the [.NET SDK](https://github.com/getsentry/sentry-dotnet/) by leaving the messy `#ifdefs`, platform specific code (operating system, runtime, etc) out into its own library. It also helps by allowing us to share code between the [new .NET SDK](https://github.com/getsentry/sentry-dotnet/) and the [current .NET SDK](https://github.com/getsentry/raven-csharp/).

Most of the platform information used by the SDK goes to Sentry's [Context Interface](https://docs.sentry.io/clientdev/interfaces/contexts/). When implementing this on SharpRaven it was clear that to get reliable information is not as trivial as it seems. This repo is an attempt to create a package which will provide reliable information in different types of apps.

## Examples

For details, check the [example project](https://github.com/getsentry/sentry-dotnet-platform-abstractions/tree/426b7b2a002738a5ccbbed644d6ccb3fa26b9eba/samples/Sentry.PlatformAbstractions.Console).

### Runtime information
If you are interested in the runtime information, with .NET Standard 1.5 onwards you can use: `RuntimeInformation.FrameworkDescription`, which will give you a **single string**

For example, .NET Core 2.0.6 on Linux returns: `.NET Core 4.6.0.0`.
Besides not telling you what was actually installed on the machine, to get the version number you would need to parse the string.

The following table compares the results of that API call to what this library returns:

|      Target      |       OS         |           RuntimeInformation.FrameworkDescription         |  This library returns an object |
| ---------------- | ---------------- | --------------------------------------------------------- | ------------------------------- |
| .NET Framework 4.7.2    |     Windows      |  .NET Framework 4.7.3101.0                                | Name: .NET Framework<br> Version: 4.7.2 |
| .NET Core 1.1.7         |      macOS       |  .NET Core 4.6.26201.01                                   | Name: .NET Core <br> Version: 1.1.7 |
| .NET Core 1.1.8         |      Linux       |  .NET Core 4.6.26328.01                                   | Name: .NET Core <br> Version: 1.1.8 |
| .NET Core 2.0.6         |      macOS       |  .NET Core 4.6.0.0                                        | Name: .NET Core <br> Version: 2.0.6 |
| .NET Core 2.0.6         |      Linux       |  .NET Core 4.6.0.0                                        | Name: .NET Core <br> Version: 2.0.6 |
| Mono 5.10.1.47          |      macOS       |   5.10.1.47 (2017-12/8eb8f7d5e74 <br>Fri Apr 13 20:18:12 EDT 2018) | Name: Mono <br> Version: Mono 5.10.1.47 |
| Mono 5.12.0.226         |      Linux       |   5.12.0.226 (tarball Thu May  3 09:48:32 UTC 2018)       | Name: Mono <br> Version: 5.12.0.226 |


It also includes extension methods to `Runtime`:

* IsMono()
* IsNetCore()
* IsNetFx()


## Supported frameworks

This library supports:

* .NET Framework 3.5 and later
* .NET Standard 1.5 and later

## Building

### Install .NET Core
.NET Core 2.0.x and 1.1.x SDKs.

### Windows
.NET Framework, 4.7.1 or later
```shell
.\build.ps1
```

### Linux and macOS
Install Mono 5.12 or later
```shell
./build.sh
```

## Resources
* [![Gitter chat](https://img.shields.io/gitter/room/getsentry/dotnet.svg)](https://gitter.im/getsentry/dotnet)
* [![Stack Overflow](https://img.shields.io/badge/stack%20overflow-sentry-green.svg)](http://stackoverflow.com/questions/tagged/sentry)
* [![Forum](https://img.shields.io/badge/forum-sentry-green.svg)](https://forum.sentry.io/c/sdks)
* Follow [@getsentry](https://twitter.com/getsentry) on Twitter for updates
