The files in this folder aren't "normal" C# files, but rather they are [Xamarin Objective-C bindings][1].
They were originally generated with [Objective Sharpie][2], using the following command:

```
sharpie bind <path to sentry-cocoa sdk root>/Sentry.xcodeproj -sdk iphoneos
```

However, the files are not purely auto-generated.  Several modifications have been made:

- Everything has been made internal, either via the `internal` keyword, or the `[Internal]` binding attribute.
- Named delegates have been replaced with `Func<T>` or `Action<T>` to work around https://github.com/xamarin/xamarin-macios/issues/15299
- `INSCopying` interfaces have been commented out, to resolve nullability error
- Items that Sharpie marked with `[Verify]` have been resolved, except for:
  - `NSErrorFromSentryError` and its containing class has been commented out, as we aren't using it presently and it needs verification

Be careful when updating these files.  They control additional code generation that happens at compile time,
which ends up under `obj/Debug/net6.0-ios/iOS/SentryCocoa`.

[1]: https://docs.microsoft.com/xamarin/cross-platform/macios/binding/objective-c-libraries
[2]: https://docs.microsoft.com/xamarin/cross-platform/macios/binding/objective-sharpie
