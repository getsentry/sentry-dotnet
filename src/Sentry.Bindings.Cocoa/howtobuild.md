Due to https://github.com/getsentry/sentry-cocoa/issues/2031, we can't use the pre-built release of the Sentry-Cocoa SDK, or our build will fail due to lack of MacCatalyst output.

To workaround, we need a custom build of Carthage, that has https://github.com/Carthage/Carthage/pull/3235 applied.
- Clone the https://github.com/NachoSoto/Carthage fork, and switch to the xcframework-catalyst branch
- run `make install` to install, which builds Carthage and installs it locally to `/usr/local/bin/carthage`.
  - See https://github.com/Carthage/Carthage/blob/master/CONTRIBUTING.md#get-started

Now go clone the https://github.com/getsentry/sentry-cocoa repo and do the following:
- Checkout the version tag that you are building.  Example: `git checkout 7.28.0`
- Run `make init`, per https://github.com/getsentry/sentry-cocoa/blob/master/CONTRIBUTING.md#setting-up-an-environment
- Modify the `makefile`, editing the `build-xcframework` section to the following:
    ```
    build-xcframework:
        @echo "--> Carthage: creating Sentry xcframework"
        /usr/local/bin/carthage build --use-xcframeworks --no-skip-current --platform ios
        /usr/local/bin/carthage build --use-xcframeworks --no-skip-current --platform macCatalyst
        ditto -c -k -X --rsrc --keepParent Carthage Sentry.xcframework.zip
    ```
- Run `make build-xcframework`
- Copy the resulting `Sentry.xcframework.zip` into the `sentry-dotnet` project at the `/lib` folder root
- Rename it to include the version number (ex: `Sentry.xcframework.7.28.0.custombuild.zip), and delete the prior version zip.
- Edit `Sentry.Bindings.Cocoa.csproj` to update `SentryCocoaSdkVersion`
- If needed, update `StructsAndEnums.cs` and `ApiDefinition.cs` (see `sharpie.md` for details).
- Run `dotnet build Sentry.Bindings.Cocoa.csproj` and resolve any warnings/errors before trying to use in the rest of the solution.
