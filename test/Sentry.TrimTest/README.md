# Sentry Trimming Test App

This project does not contain any unit or integration tests. However, if we are able to successfully publish it without 
warning/error then Sentry should be trim compatible.

To test this we need to publish the app with the following command:

```bash
dotnet publish -c Release -r <RID>
```

See:
- [Show all warnings with test app](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming#show-all-warnings-with-test-app)
- [.NET RID Catalog](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog)
