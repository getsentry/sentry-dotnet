<p align="center">
  <a href="https://sentry.io" target="_blank" align="center">
    <img src="https://sentry-brand.storage.googleapis.com/sentry-logo-black.png" width="280">
  </a>
  <br />
</p>

# Sentry.PlatformAbstractions

Branch  | AppVeyor | Travis
------------- | ------------- |-------------
dev | [![Build status](https://ci.appveyor.com/api/projects/status/arv807179rg9sg1r/branch/dev?svg=true)](https://ci.appveyor.com/project/sentry/dotnet-sentry-platform-abstractions/branch/dev) | [![Build Status](https://travis-ci.org/getsentry/dotnet-sentry-platform-abstractions.svg?branch=dev)](https://travis-ci.org/getsentry/dotnet-sentry-platform-abstractions)
master | [![Build status](https://ci.appveyor.com/api/projects/status/arv807179rg9sg1r/branch/master?svg=true)](https://ci.appveyor.com/project/sentry/dotnet-sentry-platform-abstractions/branch/master) | [![Build Status](https://travis-ci.org/getsentry/dotnet-sentry-platform-abstractions.svg?branch=master)](https://travis-ci.org/getsentry/dotnet-sentry-platform-abstractions)


## This is a work in progress. 

The idea here is to simplify the [.NET SDK](https://github.com/getsentry/sentry-dotnet/) by leaving the messy `#ifdefs`, platform specific code (operating system, runtime, etc) out into its own library. It also helps by allowing us to share code between the [new .NET SDK](https://github.com/getsentry/sentry-dotnet/) and the [current .NET SDK](https://github.com/getsentry/raven-csharp/).

Most of the platform information used by the SDK goes to Sentry's [Context Interface](https://docs.sentry.io/clientdev/interfaces/contexts/). When implementing this on SharpRaven it was clear that to get reliable information is not as trivial as it seems. This repo is an attempt to create a package which will provide reliable information in different types of apps.

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
