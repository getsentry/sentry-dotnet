# Samples

This directory includes samples using the main SDK which is part of the [Sentry](https://www.nuget.org/packages/Sentry) package and also some integrations with libraries and frameworks.

Regardless if you are interested in one of the frameworks or libraries integrations, make sure to check out also the `Console` samples.

These packages are not independant/disconnected integrations with Sentry. They all use the same underlying API to capture context data and event capture.

For example the `Console.Customized` sample will show you many of the settings which are available also if you are using the `ASP.NET Core` integration. Not necessarily all the same examples will be included in the latter.

It's worth noting that although most if not all integrations are able to **initialize** the SDK. That doesn't mean you need to provide all of them with your `DSN`. In fact, the SDK should be initialized only once. Once initialized, all the available integrations will start working together.

Make sure to check out the [documentation for more](https://docs.sentry.io/?platform=csharp).
