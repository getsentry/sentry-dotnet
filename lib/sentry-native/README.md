For now, pulled from a CI run from the Unity SDK:

https://github.com/getsentry/sentry-unity/actions/runs/6170616767

## macOS

Compiling the bridge code

```
clang -shared -framework Foundation SentryNativeBridge.m -o libBridge.dylib
```

TODO: include dSYMs

Rename Sentry.dylib to libSentry.dylib to make some .NET tooling stop crashing
```
/Applications/Xcode.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/bin/install_name_tool -id  libSentry.dylib libSentry.dylib
```
