$ErrorActionPreference = "Stop"

dotnet test -c Release
dotnet pack -c Release --no-build --include-symbols
