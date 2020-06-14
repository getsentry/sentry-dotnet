We often don't want to couple our code with static class like `SentrySdk`, especially to allow our code to be testable.
If that's your case, you can use 2 abstractions:

* @Sentry.ISentryClient
* @Sentry.IHub

The @Sentry.ISentryClient is responsible to queueing the event to be sent to Sentry and abstracting away the internal transport.
The @Sentry.IHub on the other hand, holds a client and the current scope. It in fact also implements @Sentry.ISentryClient and is able to dispatch calls to the right client depending on the current scope.

In order to allow different events hold different contextual data, you need to know in which scope you are in.
That's the job of the [`Hub`](https://github.com/getsentry/sentry-dotnet/blob/main/src/Sentry/Internal/Hub.cs). It holds the scope management as well as a client. 

If all you are doing is sending events, without modification/access to the current scope, then you depend on @Sentry.ISentryClient. If on the other hand you would like to have access to the current scope by configuring it or binding a different client to it, etc. You'd depend on `IHub`.


An example using `IHub` for testability is [SentryLogger](https://github.com/getsentry/sentry-dotnet/blob/main/src/Sentry.Extensions.Logging/SentryLogger.cs) and its unit tests [SentryLoggerTests](https://github.com/getsentry/sentry-dotnet/blob/main/test/Sentry.Extensions.Logging.Tests/SentryLoggerTests.cs).  
`SentryLogger` depends on `IHub` because it does modify the scope (through `AddBreadcrumb`). In case it only sent events, it should instead depend on `ISentryClient`
