# Sentry.PlatformAbstractions

## This is a work in progress. 

The idea here is to simplify the [.NET SDK](https://github.com/getsentry/raven-csharp/) by pulling out code used to extract platform information like operating system, runtime etc.
Most of the platform information used by the SDK goes to Sentry's [Context Interface](https://docs.sentry.io/clientdev/interfaces/contexts/). When implementing this on SharpRaven it was clear that to get reliable information is not as trivial as it seems. This repo is an attempt to create a package which will provide reliable information in different types of apps.

