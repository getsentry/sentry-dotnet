export COYOTE_CLI_TELEMETRY_OPTOUT=1
export configuration=Debug
export outputPath=bin/$configuration/coyote

dotnet build -c $configuration -o $outputPath

dotnet tool restore
dotnet tool run coyote rewrite $outputPath/Sentry.dll
dotnet tool run coyote rewrite $outputPath/Sentry.Testing.dll
dotnet tool run coyote rewrite $outputPath/Sentry.Coyote.Tests.dll

dotnet tool run coyote test $outputPath/Sentry.Coyote.Tests.dll --method EnqueueFlushAndDisposeAsync --iterations 10000
