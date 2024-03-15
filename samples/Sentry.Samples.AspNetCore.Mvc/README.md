Sample usages of the Sentry SDK for ASP.NET Core on an MVC app

Start by changing the DSN in `appsettings.json`, `Sentry:Dsn` property with your own.
No DSN yet? Get one for free at https://sentry.io/ to give this sample a run.

Blocking detection:
* It's turned on via option `CaptureBlockingCalls`
* In the `HomeController` there's an action that causes a blocking call on an async method.
* You can trigger it with:
  * `GET http://localhost:5001/home/block/true`

Results `Was blocking? True` and an event captured in Sentry.
