#!/bin/bash
set -euo pipefail

# Downloads the pre-built, self-contained SentryObjC-Dynamic.xcframework from the sentry-cocoa
# release named in modules/sentry-cocoa.properties, and lays it out exactly like the from-source
# path (build-sentry-cocoa.sh): per-platform xcframeworks under Carthage/Build-{ios,maccatalyst}
# plus the headers under Carthage/Headers used to generate the bindings.
#
# This is the default. It needs no sentry-cocoa submodule checkout and no Xcode SDK build - only
# `xcodebuild -create-xcframework` to re-slice the download down to the platforms we ship.
# The from-source toggle (build-sentry-cocoa.sh) produces the same layout. See getsentry/sentry-dotnet#5492.

FRAMEWORK="SentryObjC-Dynamic"

pushd "$(dirname "$0")" >/dev/null
SCRIPT_DIR="$PWD"
PROPS="$SCRIPT_DIR/../modules/sentry-cocoa.properties"
cd ../modules/sentry-cocoa

version=$(grep -E '^[[:space:]]*version[[:space:]]*=' "$PROPS" | sed -E 's/.*=[[:space:]]*//' | tr -d '[:space:]')
repo=$(grep -E '^[[:space:]]*repo[[:space:]]*=' "$PROPS" | sed -E 's/.*=[[:space:]]*//' | tr -d '[:space:]')
[ -n "$version" ] || { echo "No 'version' in $PROPS" >&2; exit 1; }

# Sanity: when the submodule is checked out, warn if its pinned tag doesn't match the download
# version, so the default (download) and from-source (build) paths don't silently diverge.
if [[ -e .git ]]; then
    submodule_tag=$(git describe --tags --exact-match 2>/dev/null || true)
    if [[ -n "$submodule_tag" && "$submodule_tag" != "$version" ]]; then
        echo "warning: modules/sentry-cocoa.properties version ($version) does not match the submodule tag ($submodule_tag)." >&2
    fi
fi

mkdir -p Carthage
stamp="Carthage/.downloaded-version"
zip="Carthage/$FRAMEWORK-$version.xcframework.zip"

# Skip if this version is already laid out.
if [[ -f "$stamp" && "$(cat "$stamp")" == "$version" && -d "Carthage/Build-ios/$FRAMEWORK.xcframework" ]]; then
    popd >/dev/null
    exit 0
fi

rm -rf Carthage/Build-* Carthage/Headers Carthage/extracted "$stamp"

# Download (cache the zip by version so re-runs don't re-fetch).
if [[ ! -f "$zip" ]]; then
    echo "::group::Downloading $FRAMEWORK.xcframework $version"
    curl -fSL "$repo/releases/download/$version/$FRAMEWORK.xcframework.zip" -o "$zip"
    echo "::endgroup::"
fi

mkdir -p Carthage/extracted
unzip -oq "$zip" -d Carthage/extracted
xcf="Carthage/extracted/$FRAMEWORK.xcframework"

# Re-slice down to just the platforms the .NET SDK ships, keeping the packages minimal and
# matching the from-source layout: Build-ios (device + simulator), Build-maccatalyst.
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
rm -rf Carthage/extracted

echo "$version" > "$stamp"
popd >/dev/null
