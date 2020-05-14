# Sample integration of NLog with Sentry

This is a simple console application that demonstrates how you can add Sentry to your application using NLog.

This project attempts to sample the integration by using code only and also via the configuration file.
In both cases **you need to add your own DSN** so you can see the events sent in your Sentry project.

You can get your [Sentry DSN at sentry.io](https://sentry.io).
Make sure to add it to both `NLog.config` and `Program.cs` in this directory.

## Configuration of NLog.config
The following options are available for the NLog Sentry Target:

```xml
<target xsi:type="Sentry" name="sentry"
    dsn="https://123@sentry.io/456"
    environment="${environment:cached=true:ASPNETCORE_ENVIRONMENT}"
    release="${assembly-version:cached=true:type=File}"
    layout="${message}"
    includeEventProperties="True"
    includeMdlc="False"
    breadcrumbLayout="${message}"
    minimumBreadcrumbLevel="Debug"
    minimumEventLevel="Error"
    ignoreEventsWithNoException="False"
    includeEventDataOnBreadcrumbs="False"
    includeEventPropertiesAsTags="True"
    initializeSdk="True"
    flushTimeoutSeconds="15"
    >
        <tag name="exception" layout="${exception:format=shorttype}" includeEmptyValue="false" /><!-- Repeatable SentryEvent Tags -->
        <contextproperty name="threadid" layout="${threadid}" includeEmptyValue="true" />        <!-- Repeatable SentryEvent Data -->
        <!-- Advanced options can be configured here-->
        <options
            sendDefaultPii="False"
            isEnvironmentUser="True"
            attachStacktrace="False"
        />
        <!-- Optionally specify user properties via NLog (here using MappedDiagnosticsLogicalContext as an example) -->
        <user
            id="${mdlc:item=id}" 
            username="${mdlc:item=username}"
            email="${mdlc:item=email}">
            <other name="mood" layout="joyous"/>    <!-- You can also apply additional user properties here-->
        </user>
</target>
```

* **dsn** - Sentry Data Source Name Address. See also https://sentry.io
* **initializeSdk** -  Whether the NLog target should initialize the Sentry SDK (Using Dsn). Default: _True_
* **environment** - Application Environment sent to Sentry
* **release** - Application Release Version sent to Sentry
* **layout** - NLog Layout for rendering SentryEvent message. Default: _${message}_
* **includeEventProperties** - Include LogEvent properties as Data on SentryEvent. Default: _True_
* **includeEventPropertiesAsTags** - Include LogEvent properties as extra Tags on SentryEvent. Default: _False_
* **includeMdlc** - Include NLog MDLC as Data on SentryEvent. Default: _False_
* **includeEventDataOnBreadcrumbs** - Include all event Data on breadcrumbs just like for standard SentryEvent. Default: _False_
* **breadcrumbLayout** - NLog Layout for styling the breadcrumb message. Default: Same as **layout**
* **minimumEventLevel** - Send NLog LogEvents as SentryEvent when matching severity (or worse). Default: _Error_
* **minimumBreadcrumbLevel** - Send NLog LogEvents as Breadcrumbs when matching severity (or worse). Default: _Info_
* **ignoreEventsWithNoException** - Ignore NLog LogEvents without an exception. Default: _False_
* **flushTimeoutSeconds** - Flush timeout in seconds before aborting flush to Sentry. Default: _15_
* **user**
   * **id** -
   * **username** -
   * **email** -
* **options**
   * **sendDefaultPii** - Whether to include default Personal Identifiable information (UserName / IP-Address). Default: _False_
   * **isEnvironmentUser** - Lookup Environment.User if having enabled **sendDefaultPii**. Default: _True_
   * **attachStacktrace** - Whether to send the stack trace of a event captured without an exception. Default: _False_

There is filtering logic in the Sentry Target that is usually handled by NLog Logging Rules and Filters.
Mostly because the same Sentry Target is writing both breadcrumbs and actual SentryEvents.

## Running this sample

Now you're ready to run the code.
You can run this sample with Visual Studio, or via the command line.

With the .NET Core SDK:

```sh
λ dotnet run
15:34 $ dotnet run
2019-05-13 15:34:30.7319|TRACE|Sentry.Samples.NLog.Program|Verbose message which is not sent.
2019-05-13 15:34:30.7716|DEBUG|Sentry.Samples.NLog.Program|Debug message stored as breadcrumb.
2019-05-13 15:34:30.7782|ERROR|Sentry.Samples.NLog.Program|Some event that includes the previous breadcrumbs. mood = "happy that my error is reported"
2019-05-13 15:34:30.8547|INFO|Sentry.Samples.NLog.Program|Dividing 10 by 0
2019-05-13 15:34:30.8567|WARN|Sentry.Samples.NLog.Program|a is 0
2019-05-13 15:34:30.8586|FATAL|Sentry.Samples.NLog.Program|Error: with exception. { title = compound data object, wowFactor = 11, errorReported = True }
2019-05-13 15:34:32.1986|TRACE|Sentry.Samples.NLog.Program|Verbose message which is not sent.
2019-05-13 15:34:32.1997|DEBUG|Sentry.Samples.NLog.Program|Debug message stored as breadcrumb.
2019-05-13 15:34:32.1997|ERROR|Sentry.Samples.NLog.Program|Some event that includes the previous breadcrumbs. mood = "happy that my error is reported"
2019-05-13 15:34:32.2005|INFO|Sentry.Samples.NLog.Program|Dividing 10 by 0
2019-05-13 15:34:32.2005|WARN|Sentry.Samples.NLog.Program|a is 0
2019-05-13 15:34:32.2010|FATAL|Sentry.Samples.NLog.Program|Error: with exception. { title = compound data object, wowFactor = 11, errorReported = True }
```

![Sample event in Sentry](.assets/nlog-sentry.png)

**Please refer to the main SDK [documentation for more details](https://getsentry.github.io/sentry-dotnet/).**
