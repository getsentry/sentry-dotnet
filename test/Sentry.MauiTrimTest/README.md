# Sentry MAUI Trimming Test App

This project is used to ensure Sentry is trim compatible when targeting iOS and Android. It does not contain any unit or 
integration tests. However, if we are able to successfully publish then the SDK should be trim compatible.

The following commands can be used to test the bindings:

```bash
dotnet publish test/Sentry.MauiTrimTest/Sentry.MauiTrimTest.csproj -c Release -f net9.0-ios18.0 -r ios-arm64
dotnet publish test/Sentry.MauiTrimTest/Sentry.MauiTrimTest.csproj -c Release -f net9.0-android35.0 -r android-arm64
```

See:
- [Show all warnings with test app](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming#show-all-warnings-with-test-app)
- [.NET RID Catalog](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog)
