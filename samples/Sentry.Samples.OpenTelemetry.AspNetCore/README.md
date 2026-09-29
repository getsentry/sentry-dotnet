# Overview

This sample demonstrates how an ASP.NET Core application that is instrumented with the OpenTelemetry .NET SDK can be 
configured to send trace information to Sentry.

## Customizing propagation

Sentry's OpenTelemetry integration sets the DefaultTextMapPropagator for OpenTelemetry to a `SentryPropagator`. This 
propagator ensures that both the W3C baggage header and the sentry-trace header get propagated from upstream services 
and/or to downstream services. 

If you need to further customize header propagation in your application (e.g. propagating other vendor specific headers)
then you can do so by creating a `CompositeTextMapPropagator` consisting of the custom propagator(s) you need plus the
`SentryPropagator`. You can supply this as an optional parameter to the `AddSentryOtlpExporter` method.
