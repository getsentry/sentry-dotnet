
## Installing the integration on your app

Using NuGet:

```powershell
Install-Package Sentry.Log4Net
```

Or using the .NET Core CLI:

```sh
dotnet add Sentry.Log4Net
```

## Configuration

Once the log4net integration package is installed on your project, you can modify your configuration file to add the appender.
This can be done, for example, via the `app.config` or `web.config` in case of ASP.NET.

```xml
  <appender name="SentryAppender" type="Sentry.Log4Net.SentryAppender, Sentry.Log4Net">
      <Dsn value="dsn"/>
      <!--Sends the log event Identity value as the user-->
      <SendIdentity value="true" />
      <Environment value="dev" />
      <threshold value="INFO" />
    </appender>
```

For how it's done in this sample, please refer to [sample app.config](https://github.com/getsentry/sentry-dotnet/blob/main/samples/Sentry.Samples.Log4Net/app.config).

The example above defines the [DSN](https://docs.sentry.io/quickstart/#configure-the-dsn) so that the `SentryAppender` is able to initialize the SDK.

This is only one of the options. If you wish to configure the SDK manually in the app before creating the logging integration, you could **leave the DSN out** of the log4net configuration file and call:

```csharp
SentrySdk.Init("DSN");
```

One of the advantages of this approach is that you can pass multiple configurations via the `Init` method. 

Bottom line is that the SDK needs to be initialized only **once** so you can choose where the initialization will happen. Other integrations (like ASP.NET) also is able to initialize the SDK. Make sure you pass the DSN to only one of these integrations, or if you are calling `Init` by yourself, there's no need to pass the DSN to the integration.

Please refer to [the sample](https://github.com/getsentry/sentry-dotnet/tree/main/samples/Sentry.Samples.Log4Net) to see it in action.

![Sample event in Sentry](https://github.com/getsentry/sentry-dotnet/blob/main/samples/Sentry.Samples.Log4Net/.assets/log4net-sample.gif?raw=true)
