# Sentry MAUI Trimming Test App

This project is used to ensure Sentry is trim compatible when targeting iOS and Android. It does not contain any unit or 
integration tests. However, if we are able to successfully publish then the SDK should be trim compatible.

The following commands, run from the repository root, can be used to test the bindings. Like the `trim-analysis` job
in `.github/workflows/build.yml`, they read the TFMs from `Directory.Build.props`:

```bash
dotnet publish test/Sentry.MauiTrimTest/Sentry.MauiTrimTest.csproj -c Release -f "$(dotnet msbuild Directory.Build.props -getProperty:PreviousIosTfm)" -r ios-arm64
dotnet publish test/Sentry.MauiTrimTest/Sentry.MauiTrimTest.csproj -c Release -f "$(dotnet msbuild Directory.Build.props -getProperty:LatestIosTfm)" -r ios-arm64
dotnet publish test/Sentry.MauiTrimTest/Sentry.MauiTrimTest.csproj -c Release -f "$(dotnet msbuild Directory.Build.props -getProperty:LatestIosTfm)" -r ios-arm64 -p:MtouchLink=Full
dotnet publish test/Sentry.MauiTrimTest/Sentry.MauiTrimTest.csproj -c Release -f "$(dotnet msbuild Directory.Build.props -getProperty:PreviousAndroidTfm)" -r android-arm64
dotnet publish test/Sentry.MauiTrimTest/Sentry.MauiTrimTest.csproj -c Release -f "$(dotnet msbuild Directory.Build.props -getProperty:LatestAndroidTfm)" -r android-arm64
```

The project trims iOS partially, which is the setup that crashed in
[#5855](https://github.com/getsentry/sentry-dotnet/issues/5855). The `-p:MtouchLink=Full` publish overrides that with
the full trimming that NativeAOT apps get by default.

The iOS app is published unsigned, so no certificate or provisioning profile is needed.

See:
- [Show all warnings with test app](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming#show-all-warnings-with-test-app)
- [.NET RID Catalog](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog)
