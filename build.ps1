$ErrorActionPreference = "Stop"

dotnet restore --locked-mode
 if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet test --no-restore -c Release
 if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet pack -c Release --no-build
 if ($LASTEXITCODE -ne 0) { exit 1 }
