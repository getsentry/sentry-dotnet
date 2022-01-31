# Contributing

We love receiving PRs from the community with features and fixed. 
For big feature it's advised to raise an issue to discuss it first.

## TLDR: 

* Install [the .NET Core SDK](https://dot.net/) 
* To quickly get up and running, you can just run `dotnet build`
* To run a full build and test locally before pushing, run `./build.sh` or `./buld.ps1`

## Dependencies

* The .NET Core SDK
* On Windows: .NET Framework 4.6.2 or higher.
* On macOS/Linux: Mono 6 or higher if you expect to run the unit tests on the `net4x` targets.

## API changes approval process

This repository uses [Verify](https://github.com/VerifyTests/Verify) to store the public API diffs in snapshot files.
When a change involves modifying the public API area (by for example adding a public method),
you'll need to approve the changes otherwise the CI process will fail.

To do that, run the build locally (i.e: `./build.sh` or `pwsh .\build.ps1`)
and commit the `verify` files that were changed.
