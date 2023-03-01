The files in this folder aren't "normal" C# files, but rather they are [Xamarin Objective-C bindings][1].
They are generated using [Objective Sharpie][2], using the script in `../scripts/generate-cocoa-bindings.ps1`.

Do not modify the `.cs` files directly.  Instead, update the script as needed and re-generate.

[1]: https://docs.microsoft.com/xamarin/cross-platform/macios/binding/objective-c-libraries
[2]: https://docs.microsoft.com/xamarin/cross-platform/macios/binding/objective-sharpie
