Files from force's `Crc32.NET` at `26c5a818a5c7a3d6a622c92d3cd08dba586c263c` copied:

https://github.com/force-net/Crc32.NET/commit/26c5a818a5c7a3d6a622c92d3cd08dba586c263c

Sentry's core package's goal is to be dependency-free. Because of that we use different strategies of vendoring-in code.
`Ben.Demystifier` for example comes in through a git submodule, and a commit changing all types to `internal` is added.

Here, since no changes were done to this project in years, and it's just a handful of files,
we're directly copying and changing them.

Main changes:

* Make everything internal.
