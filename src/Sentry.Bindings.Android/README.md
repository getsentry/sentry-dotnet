This package supports the Sentry SDK for .NET.  It is not intended to be referenced directly.
Instead, reference one of the following:

- [`Sentry.Maui`](https://www.nuget.org/packages/Sentry.Maui) - if you are using .NET MAUI
- [`Sentry`](https://www.nuget.org/packages/Sentry) - if you are authoring a .NET Android application without using MAUI

## SDK Developers and Contributors

For .NET SDK contributors, most of the classes in this package are generated automatically by proguard.

- Proguard configuration is defined in `sentry-proguard.cfg` (see [Proguard usage](https://www.guardsquare.com/manual/configuration/usage))

- Post generation transformations are controlled via various XML files stored in the `/Transforms` directory  (see [Java Bindings Metadata documentation](https://learn.microsoft.com/en-gb/previous-versions/xamarin/android/platform/binding-java-library/customizing-bindings/java-bindings-metadata) for details).
