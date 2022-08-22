using (SentrySdk.Init("https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537"))
{
    // The following exception is captured and sent to Sentry
    SomeMethod();
}

void SomeMethod()
{
    AnotherMethod();
}

void AnotherMethod()
{
    throw null;
}