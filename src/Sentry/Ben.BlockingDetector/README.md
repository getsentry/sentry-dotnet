Files from Ben Adam's `Ben.BlockingDetector` at `fce6c534dfcee6ba8cb3ef74d246211b81f5cbeb` copied:

https://github.com/benaadams/Ben.BlockingDetector/blob/fce6c534dfcee6ba8cb3ef74d246211b81f5cbeb/

Sentry's core package's goal is to be dependency-free. Because of that we use different strategies of vendoring-in code.
`Ben.Demystifier` for comes in through git submodule, and a commit changing all types to `internal` is added.

Here, since no changes were done to this project in years, and it's just a handful of files, 
we're directly copying and changing them.

Main changes:

* Remove dependency to `ILogger`
* Make everything internal.
