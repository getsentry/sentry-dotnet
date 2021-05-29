export COYOTE_CLI_TELEMETRY_OPTOUT=1
export tfm=net5.0
export configuration=Debug

dotnet build -c $configuration
# dotnet tool restore
dotnet tool run coyote rewrite bin/$configuration/$tfm/Sentry.dll
dotnet tool run coyote rewrite bin/$configuration/$tfm/Sentry.*.dll

dotnet tool run coyote test bin/$configuration/$tfm/Sentry.Coyote.Tests.dll --method EnqueueFlushAndDispose --iterations 10000``

# string name signing doesn't work on macOS
#COYOTE_CLI_TELEMETRY_OPTOUT=1 \
#  dotnet tool run coyote rewrite bin/Debug/net5.0/Sentry.dll \
#  --strong-name-key-file ../../.assets/Sentry.snk
