# Sentry Trimming Test App

This project is used to ensure Sentry packages are trim compatible when targeting non-mobile targets.

This project does not contain any unit or integration tests. However, if we are able to successfully publish it without 
warning/error then Sentry should be trim compatible.

To test this we need to publish the app by running `dotnet publish -c Release -r <RID>`. For example, on macOS:

```bash
dotnet publish -c Release -r oxs-arm64
```

See:
- [Show all warnings with test app](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming#show-all-warnings-with-test-app)
- [.NET RID Catalog](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog)
