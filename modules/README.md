# Git Submodules

Our submodules point to repositories in the getsentry org. Either they are Sentry repos, or forked into the org.

### Ben.Demystifier

Fork [getsentry/Ben.Demystifier](https://github.com/getsentry/Ben.Demystifier). 
From the popular library from Ben Adams: [Ben.Demystifier](https://github.com/benaadams/Ben.Demystifier)

The commit checked out on this fork always has changes applied after running the [make-internal.sh](make-internal.sh) script.
To make sure we're not exposing any API from that library externally. 

### perfview

Fork [getsentry/perfview](https://github.com/getsentry/perfview/).
Tool from the .NET team which includes several utilities used for profiling .NET code. 
We use that in our `Sentry.Profiling` package.

### sentry-cocoa

By default, `Sentry.Bindings.Cocoa` downloads a pre-built Sentry Cocoa SDK from
GitHub Releases. The version is specified in `sentry-cocoa.properties`.

If you want to build an unreleased Sentry Cocoa SDK version from source instead,
replace the pre-built SDK with [getsentry/sentry-cocoa](https://github.com/getsentry/sentry-cocoa/)
by cloning it into the `modules/sentry-cocoa` directory. To switch back to the
pre-built SDK, delete the `modules/sentry-cocoa` directory.
