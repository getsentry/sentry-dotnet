using (SentrySdk.Init(o =>
       {
           o.Dsn = "http://739dbeb34c914c2697716c357e7a2a1d@hackweek-release-reboot.ngrok.io/1";
           o.Debug = true;
           o.AutoSessionTracking = true;
           o.ShutdownTimeout = TimeSpan.FromSeconds(30);
       }))
{
    // The following exception is captured and sent to Sentry
    // throw null;
    Console.WriteLine("aline");
}
