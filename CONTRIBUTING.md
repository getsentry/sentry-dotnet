# Contributing

We love receiving PRs from the community with features and fixed. 
For big feature it's advised to raise an issue to discuss it first.

## TLDR: 

* Install [the .NET Core SDK](https://dot.net/) 
* run `./build.sh` on macOS/Linux or `powershell ./build.ps1` on Windows.

## Dependencies

* The .NET Core SDK (version that is pinned on `global.json` is advised but not required, remove the file if needed).
* On Windows: .NET Framework 4.6.2 or higher.
* On macOS/Linux: Mono 6 or higher if you expexct to run the unit tests on the `net4x` targets.
