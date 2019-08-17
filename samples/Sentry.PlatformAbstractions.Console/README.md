# PlatformAbstractions sample console

Running it will output to console some of the APIs defined in the library.


# Windows
```shell
dotnet --info
.NET Command Line Tools (2.1.104)

Product Information:
 Version:            2.1.104
 Commit SHA-1 hash:  48ec687460

Runtime Environment:
 OS Name:     Windows
 OS Version:  10.0.17134
 OS Platform: Windows
 RID:         win10-x64
 Base Path:   C:\Program Files\dotnet\sdk\2.1.104\

Host (useful for support):
  Version: 2.1.0-rc1
  Commit:  eb9bc92051

.NET Core SDKs installed:
  1.1.6 [C:\Program Files\dotnet\sdk]
  1.1.7 [C:\Program Files\dotnet\sdk]
  1.1.8 [C:\Program Files\dotnet\sdk]
  2.0.3 [C:\Program Files\dotnet\sdk]
  2.1.100 [C:\Program Files\dotnet\sdk]
  2.1.101 [C:\Program Files\dotnet\sdk]
  2.1.104 [C:\Program Files\dotnet\sdk]
  2.1.200-preview-007474 [C:\Program Files\dotnet\sdk]
  2.1.200-preview-007597 [C:\Program Files\dotnet\sdk]
  2.1.200 [C:\Program Files\dotnet\sdk]
  2.1.300-preview2-008533 [C:\Program Files\dotnet\sdk]
  2.1.300-rc1-008673 [C:\Program Files\dotnet\sdk]

.NET Core runtimes installed:
  Microsoft.AspNetCore.All 2.1.0-preview2-final [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.All]
  Microsoft.AspNetCore.All 2.1.0-rc1-final [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.All]
  Microsoft.AspNetCore.App 2.1.0-preview2-final [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 2.1.0-rc1-final [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
  Microsoft.NETCore.App 1.0.8 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 1.0.9 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 1.0.10 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 1.1.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 1.1.6 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 1.1.7 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.0.6 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.0.7 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.1.0-preview2-26406-04 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.1.0-rc1 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
```
```shell
λ run.cmd
Running any already built Console sample:

Running: C:\Users\bruno\git\dotnet-sentry-platform-abstractions\samples\Sentry.PlatformAbstractions.Console\bin\Release\net35\Sentry.PlatformAbstractions.Console.exe
Runtime.Current:

    ToString():               .NET Framework 3.5 SP 1

    Name:                     .NET Framework
    Version:                  3.5 SP 1
    Raw:                      Environment.Version=2.0.50727.8922

Runtime.Current.FrameworkInstallation:
    ShortName:                v3.5
    Profile:
    Version:                  3.5.30729.4926
    ServicePack:              1
    Release:
    ToString():               3.5.30729

Extension methods on Runtime:
    IsMono():                 False
    IsNetCore()               False
    IsNetFx():                True


Running: C:\Users\bruno\git\dotnet-sentry-platform-abstractions\samples\Sentry.PlatformAbstractions.Console\bin\Release\net471\Sentry.PlatformAbstractions.Console.exe
Runtime.Current:

    ToString():               .NET Framework 4.7.2

    Name:                     .NET Framework
    Version:                  4.7.2
    Raw:                      .NET Framework 4.7.3101.0

Runtime.Current.FrameworkInstallation:
    ShortName:
    Profile:
    Version:                  4.7.2
    ServicePack:
    Release:                  461808
    ToString():               4.7.2

Extension methods on Runtime:
    IsMono():                 False
    IsNetCore()               False
    IsNetFx():                True


Running: C:\Users\bruno\git\dotnet-sentry-platform-abstractions\samples\Sentry.PlatformAbstractions.Console\bin\Release\netcoreapp1.1\Sentry.PlatformAbstractions.Console.dll
Runtime.Current:

    ToString():               .NET Core 1.1.7

    Name:                     .NET Core
    Version:                  1.1.7
    Raw:                      .NET Core 4.6.26201.01
Extension methods on Runtime:
    IsMono():                 False
    IsNetCore()               True
    IsNetFx():                False


Running: C:\Users\bruno\git\dotnet-sentry-platform-abstractions\samples\Sentry.PlatformAbstractions.Console\bin\Release\netcoreapp2.0\Sentry.PlatformAbstractions.Console.dll
Runtime.Current:

    ToString():               .NET Core 2.0.7

    Name:                     .NET Core
    Version:                  2.0.7
    Raw:                      .NET Core 4.6.26328.01
Extension methods on Runtime:
    IsMono():                 False
    IsNetCore()               True
    IsNetFx():                False

```

# Linux (Ubuntu)

```shell
$ mono --version
Mono JIT compiler version 5.12.0.226 (tarball Thu May  3 09:48:32 UTC 2018)
Copyright (C) 2002-2014 Novell, Inc, Xamarin Inc and Contributors. www.mono-project.com
        TLS:           __thread
        SIGSEGV:       altstack
        Notifications: epoll
        Architecture:  amd64
        Disabled:      none
        Misc:          softdebug
        Interpreter:   yes
        LLVM:          supported, not enabled.
        GC:            sgen (concurrent by default)

$ dotnet --info
.NET Command Line Tools (2.1.101)

Product Information:
 Version:            2.1.101
 Commit SHA-1 hash:  6c22303bf0

Runtime Environment:
 OS Name:     ubuntu
 OS Version:  16.04
 OS Platform: Linux
 RID:         ubuntu.16.04-x64
 Base Path:   /usr/share/dotnet/sdk/2.1.101/

Host (useful for support):
  Version: 2.1.0-rc1
  Commit:  eb9bc92051

.NET Core SDKs installed:
  1.1.9 [/usr/share/dotnet/sdk]
  2.0.0 [/usr/share/dotnet/sdk]
  2.1.101 [/usr/share/dotnet/sdk]
  2.1.300-rc1-008673 [/usr/share/dotnet/sdk]

.NET Core runtimes installed:
  Microsoft.AspNetCore.All 2.1.0-rc1-final [/usr/share/dotnet/shared/Microsoft.AspNetCore.All]
  Microsoft.AspNetCore.App 2.1.0-rc1-final [/usr/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.NETCore.App 1.0.10 [/usr/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 1.0.11 [/usr/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 1.1.7 [/usr/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 1.1.8 [/usr/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.0.0 [/usr/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.0.6 [/usr/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.1.0-rc1 [/usr/share/dotnet/shared/Microsoft.NETCore.App]

To install additional .NET Core runtimes or SDKs:
  https://aka.ms/dotnet-download
```

## Sample output
```shell
$ ./run.sh
Running: bin/Release/netcoreapp1.1/Sentry.PlatformAbstractions.Console.dll
Runtime.Current:

    ToString():               .NET Core 1.1.8

    Name:                     .NET Core
    Version:                  1.1.8
    Raw:                      .NET Core 4.6.26328.01
Extension methods on Runtime:
    IsMono():                 False
    IsNetCore()               True
    IsNetFx():                False


Running: bin/Release/netcoreapp2.0/Sentry.PlatformAbstractions.Console.dll
Runtime.Current:

    ToString():               .NET Core 2.0.6

    Name:                     .NET Core
    Version:                  2.0.6
    Raw:                      .NET Core 4.6.0.0
Extension methods on Runtime:
    IsMono():                 False
    IsNetCore()               True
    IsNetFx():                False


Running: bin/Release/net35/Sentry.PlatformAbstractions.Console.exe
WARNING: The runtime version supported by this application is unavailable.
Using default runtime: v4.0.30319
Runtime.Current:

    ToString():               Mono 5.12.0.226

    Name:                     Mono
    Version:                  5.12.0.226
    Raw:                      5.12.0.226 (tarball Thu May  3 09:48:32 UTC 2018)

Runtime.Current.FrameworkInstallation:
    ShortName:
    Profile:
    Version:
    ServicePack:
    Release:
    ToString():

Extension methods on Runtime:
    IsMono():                 True
    IsNetCore()               False
    IsNetFx():                False


Running: bin/Release/net471/Sentry.PlatformAbstractions.Console.exe
Runtime.Current:

    ToString():               Mono 5.12.0.226

    Name:                     Mono
    Version:                  5.12.0.226
    Raw:                      Mono 5.12.0.226 (tarball Thu May  3 09:48:32 UTC 2018)

Runtime.Current.FrameworkInstallation:
    ShortName:
    Profile:
    Version:
    ServicePack:
    Release:
    ToString():

Extension methods on Runtime:
    IsMono():                 True
    IsNetCore()               False
    IsNetFx():                False
```

# macOS

```shell
$ mono --version
Mono JIT compiler version 5.10.1.47 (2017-12/8eb8f7d5e74 Fri Apr 13 20:18:12 EDT 2018)
Copyright (C) 2002-2014 Novell, Inc, Xamarin Inc and Contributors. www.mono-project.com
	TLS:           normal
	SIGSEGV:       altstack
	Notification:  kqueue
	Architecture:  amd64
	Disabled:      none
	Misc:          softdebug
	Interpreter:   yes
	LLVM:          yes(3.6.0svn-mono-master/8b1520c8aae)
	GC:            sgen (concurrent by default)


$ dotnet --info
.NET Command Line Tools (2.1.101)

Product Information:
 Version:            2.1.101
 Commit SHA-1 hash:  6c22303bf0

Runtime Environment:
 OS Name:     Mac OS X
 OS Version:  10.13
 OS Platform: Darwin
 RID:         osx.10.12-x64
 Base Path:   /usr/local/share/dotnet/sdk/2.1.101/

Host (useful for support):
  Version: 2.1.0-rc1
  Commit:  eb9bc92051

.NET Core SDKs installed:
  1.1.8 [/usr/local/share/dotnet/sdk]
  2.1.4 [/usr/local/share/dotnet/sdk]
  2.1.101 [/usr/local/share/dotnet/sdk]
  2.1.300-rc1-008673 [/usr/local/share/dotnet/sdk]

.NET Core runtimes installed:
  Microsoft.AspNetCore.All 2.1.0-rc1-final [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.All]
  Microsoft.AspNetCore.App 2.1.0-rc1-final [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.NETCore.App 1.0.10 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 1.1.7 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.0.5 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.0.6 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.1.0-rc1 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
```

## Sample output
```shell
$ ./run.sh
Running: bin/Release/netcoreapp1.1/Sentry.PlatformAbstractions.Console.dll
Runtime.Current:

    ToString():               .NET Core 1.1.7

    Name:                     .NET Core
    Version:                  1.1.7
    Raw:                      .NET Core 4.6.26201.01
Extension methods on Runtime:
    IsMono():                 False
    IsNetCore()               True
    IsNetFx():                False


Running: bin/Release/netcoreapp2.0/Sentry.PlatformAbstractions.Console.dll
Runtime.Current:

    ToString():               .NET Core 2.0.6

    Name:                     .NET Core
    Version:                  2.0.6
    Raw:                      .NET Core 4.6.0.0
Extension methods on Runtime:
    IsMono():                 False
    IsNetCore()               True
    IsNetFx():                False


Running: bin/Release/net35/Sentry.PlatformAbstractions.Console.exe
WARNING: The runtime version supported by this application is unavailable.
Using default runtime: v4.0.30319
Runtime.Current:

    ToString():               Mono 5.10.1.47

    Name:                     Mono
    Version:                  5.10.1.47
    Raw:                      5.10.1.47 (2017-12/8eb8f7d5e74 Fri Apr 13 20:18:12 EDT 2018)

Runtime.Current.FrameworkInstallation:
    ShortName:
    Profile:
    Version:
    ServicePack:
    Release:
    ToString():

Extension methods on Runtime:
    IsMono():                 True
    IsNetCore()               False
    IsNetFx():                False


Running: bin/Release/net471/Sentry.PlatformAbstractions.Console.exe
Runtime.Current:

    ToString():               Mono 5.10.1.47

    Name:                     Mono
    Version:                  5.10.1.47
    Raw:                      Mono 5.10.1.47 (2017-12/8eb8f7d5e74 Fri Apr 13 20:18:12 EDT 2018)

Runtime.Current.FrameworkInstallation:
    ShortName:
    Profile:
    Version:
    ServicePack:
    Release:
    ToString():

Extension methods on Runtime:
    IsMono():                 True
    IsNetCore()               False
    IsNetFx():                False

```
