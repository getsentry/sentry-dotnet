For now, pulled from a CI run from the Unity SDK:

https://github.com/getsentry/sentry-unity/actions/runs/6170616767

## macOS

Compiling the bridge code

```
clang -arch x86_64 -arch arm64 -shared -framework Foundation SentryNativeBridge.m -o libBridge.dylib
```
TODO: include dSYMs

Rename Sentry.dylib to libSentry.dylib to make some .NET tooling stop crashing
```
mv Sentry.dylib libSentry.dylib
mv Sentry.dylib.dSYM libSentry.dylib.dSYM
/Applications/Xcode.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/bin/install_name_tool -id  libSentry.dylib libSentry.dylib
```
