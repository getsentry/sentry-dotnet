#!/bin/bash
set -euo pipefail

# From-source toggle for the Cocoa SDK (-p:BuildCocoaSdkFromSource=true). Builds the self-contained
# SentryObjC-Dynamic.xcframework from the modules/sentry-cocoa submodule using sentry-cocoa's own
# packaging pipeline, then lays it out exactly like the default download path
# (download-sentry-cocoa.sh): per-platform xcframeworks under Carthage/Build-{ios,maccatalyst} plus
# the headers under Carthage/Headers used to generate the bindings.
#
# This exists so we can add native debug logging / patch the SDK while diagnosing an issue. It
# produces the same artifact the default path downloads, so runtime behaviour is identical either
# way. See getsentry/sentry-dotnet#5492.

FRAMEWORK="SentryObjC-Dynamic"
SDKS="iphoneos,iphonesimulator,maccatalyst"

# Include this script's own hash in the build stamp so cached output is rebuilt whenever the recipe
# changes, not just when the sentry-cocoa submodule moves.
script_checksum=$(shasum -a 256 "$0" | cut -d ' ' -f 1)

pushd "$(dirname "$0")" >/dev/null
cd ../modules/sentry-cocoa

mkdir -p Carthage
PID_FILE="$PWD/Carthage/.build.pid"
trap 'if [[ "$(cat "$PID_FILE" 2>/dev/null)" == "$$" ]]; then rm -f "$PID_FILE"; fi' EXIT

# Serialize concurrent invocations; parallel builds race on DerivedData / SPM caches.
TMP_FILE=$(mktemp "$PID_FILE.tmp.XXXXXX")
echo $$ > "$TMP_FILE"
while ! ln "$TMP_FILE" "$PID_FILE" 2>/dev/null; do
    build_pid=$(cat "$PID_FILE" 2>/dev/null || true)
    if [[ -n "$build_pid" ]] && ! kill -0 "$build_pid" 2>/dev/null; then
        echo "Previous build did not complete (pid $build_pid); cleaning up and retrying" >&2
        if mv "$PID_FILE" "$PID_FILE.stale.$$" 2>/dev/null; then
            rm -f "$PID_FILE.stale.$$"
        fi
        continue
    fi
    sleep 2
done
rm -f "$TMP_FILE"

build_stamp="$(git rev-parse HEAD) $script_checksum"
if [[ -f Carthage/.built-from-sha ]] && [[ "$(cat Carthage/.built-from-sha)" == "$build_stamp" ]]; then
    popd >/dev/null
    exit 0
fi

rm -rf Carthage/Build-* Carthage/Headers Carthage/.built-from-sha Carthage/.downloaded-version

# Build the self-contained SentryObjC-Dynamic.xcframework via sentry-cocoa's own packaging script
# (compiles the SDK to a static lib per SDK, then relinks into a dynamic framework).
echo "::group::Building SentryObjC-Dynamic from source ($SDKS)"
./scripts/build-xcframework-sentryobjc.sh --variant dynamic --sdks "$SDKS"
echo "::endgroup::"

# build-xcframework-sentryobjc.sh writes the assembled xcframework to the submodule root.
xcf="$FRAMEWORK.xcframework"
[ -d "$xcf" ] || { echo "Expected $PWD/$xcf was not produced" >&2; exit 1; }

# Re-slice into just the platforms the .NET SDK ships, matching the download layout:
#   Build-ios (device + simulator), Build-maccatalyst.
xcodebuild -create-xcframework \
    -framework "$xcf/ios-arm64/SentryObjC.framework" \
    -framework "$xcf/ios-arm64_x86_64-simulator/SentryObjC.framework" \
    -output "Carthage/Build-ios/$FRAMEWORK.xcframework"
xcodebuild -create-xcframework \
    -framework "$xcf/ios-arm64_x86_64-maccatalyst/SentryObjC.framework" \
    -output "Carthage/Build-maccatalyst/$FRAMEWORK.xcframework"

# Copy headers (used to generate bindings) before we strip them from the bundled frameworks.
mkdir -p Carthage/Headers
find "Carthage/Build-ios/$FRAMEWORK.xcframework/ios-arm64" -name '*.h' -exec cp {} Carthage/Headers \;

# Don't bundle headers/modules in the nuget package.
find Carthage/Build* \( -name Headers -o -name PrivateHeaders -o -name Modules \) -exec rm -rf {} +
rm -rf XCFrameworkBuildPath "$FRAMEWORK.xcframework"

echo "$build_stamp" > Carthage/.built-from-sha
popd >/dev/null
