# Manual

Besides the examples documented here, you can refer to [samples on GitHub](https://github.com/getsentry/sentry-dotnet/tree/main/samples). 

These are complete samples which you can run with a debugger to see how the SDK works.

### Static use

The SDK provides a static entry point class called @Sentry.SentrySdk.

### Initialize the SDK

@SentrySdk.Init

Once the SDK is initialized, unhandled exceptions will automatically be captured and sent to Sentry.
More context can be added, for example, breadcrumbs:

`SentrySdk.AddBreadcrumb("User accepted TOC");`

By default, the last 100 breadcrumbs are kept. This is configurable alongside many other settings via a parameter to the `Init` method.

Breadcrumbs are attached to the scope. See further to understand Scopes.

### Scope management

The scope is a lightweight object that exists in memory since the SDK is initialized. It can be used to augment events sent to Sentry.
When the SDK is initialized, an empty Scope is already put in memory. That can be modified by you so that any event sent, regardless from where they are sent (e.g: a Logger integration that you configured) will include that scope data.
You can create new scopes, which will clone the previous but will be totally isolated from it.

The scope can be configured through:

```csharp
SentrySdk.ConfigureScope(scope =>
{
    scope.SetTag("Example", "Sentry docs");
}
```

There's also an asynchronous version if you need to do some I/O or run other TPL based work in order to retrieve the value to modify the scope:

```csharp
await SentrySdk.ConfigureScopeAsync(async scope =>
{
    // Anonymous object containing user retrieved from the DB
    scope.SetExtra("SomeExtraInfo",
        new
        {
            Data = "Value fetched asynchronously",
            User = await _repository.GetUserId(id);
        });
});
```

> `ConfigureScope` and `ConfigureScopeAsync` can be called as many times as you need. 
> It'll invoke your callback with the current scope, allowing you to modify it further.

To push a new scope into the stack and isolate any modifications from other scope, you can call: `PushScope` and call `Dispose` to drop it.

```csharp
using (SentrySdk.PushScope())
{
    SentrySdk.ConfigureScope(s => s.User = new User("name"));

    Work(); // If an event happens inside this method, the user set above is sent with it.
}

// Disposed the scope above, User is no longer in the scope!
```

### Release

The Sentry release feature (see [Sentry docs to learn about it](https://docs.sentry.io/learn/releases/)) requires the SDK to send the actual
application release number. That is done via the `SentryEvent` property called `Release`.

#### Automatically discovering release version

The SDK attempts to locate the release and add that to every event sent out to Sentry.

> [Default values like 1.0 or 1.0.0.0 are ignored](https://github.com/getsentry/sentry-dotnet/blob/dbb5a3af054d0ca6f801de37fb7db3632ca2c65a/src/Sentry/Internal/ApplicationVersionLocator.cs#L14-L21).

It will firstly look at the [entry assembly's](https://msdn.microsoft.com/en-us/library/system.reflection.assembly.getentryassembly(v=vs.110).aspx) @System.Reflection.AssemblyInformationalVersionAttribute, which accepts a string as
value as is often used to set the GIT commit hash. 

If that returns null, it'll look at the default @System.Reflection.AssemblyVersionAttribute which accepts the numeric version number. When creating a project with Visual Studio, usually that includes version *1.0.0.0*.
Since that usually means that the version is either not being set, or is set via a different method. The **automatic version detection will disregard** this value and no *Release* will be reported automatically.
