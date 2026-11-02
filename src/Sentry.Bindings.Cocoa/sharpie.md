The files in this folder aren't "normal" C# files, but rather they are [Xamarin Objective-C bindings][1].
They are generated using [Objective Sharpie][2], using the script in `../scripts/generate-cocoa-bindings.ps1`.

Do not modify the `.cs` files directly.  Instead, update the script as needed and re-generate.
`Sentry.Bindings.Cocoa.csproj` re-runs the generation script on every macOS build whose Cocoa headers or scripts are newer than these files, and CI then fails on any difference between the regenerated output and what is committed - so a hand-edit will be overwritten locally and rejected by CI. Mechanical fixups to Sharpie's raw output belong in `../scripts/patch-cocoa-bindings.cs`, which the generation script applies afterwards.

Objective Sharpie is the [`Sharpie.Bind.Tool`][3] dotnet tool, pinned in `/.config/dotnet-tools.json` and invoked as `dotnet sharpie`; the generation script runs `dotnet tool restore` first, so no manual install is needed. Pinning it matters because the generated output shifts between Sharpie versions - bump the manifest deliberately, then re-generate and commit the result.

[1]: https://docs.microsoft.com/xamarin/cross-platform/macios/binding/objective-c-libraries
[2]: https://docs.microsoft.com/xamarin/cross-platform/macios/binding/objective-sharpie
[3]: https://www.nuget.org/packages/Sharpie.Bind.Tool
