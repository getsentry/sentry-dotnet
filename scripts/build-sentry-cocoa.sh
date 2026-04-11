#!/bin/bash
set -euo pipefail

pushd "$(dirname "$0")" >/dev/null
cd ../modules/sentry-cocoa

mkdir -p Carthage
LOCK_DIR="$PWD/Carthage/.lock.d"

# Serialize concurrent invocations; parallel xcodebuilds race on DerivedData.
LOCK_HELD=0
trap 'if [[ $LOCK_HELD -eq 1 ]]; then rmdir "$LOCK_DIR" 2>/dev/null || true; fi' EXIT
while ! mkdir "$LOCK_DIR" 2>/dev/null; do
    sleep 0.2
done
LOCK_HELD=1

if [[ -f Carthage/.built-from-sha ]] && [[ "$(cat Carthage/.built-from-sha)" == "$(git rev-parse HEAD)" ]]; then
    popd >/dev/null
    exit 0
fi

rm -rf Carthage/output-*.xcarchive Carthage/Build-* Carthage/Headers Carthage/.built-from-sha

# Grabbing the first SDK versions
sdks=$(xcodebuild -showsdks)
ios_sdk=$(echo "$sdks" | awk '/iOS SDKs/{getline; print $NF}')
ios_simulator_sdk=$(echo "$sdks" | awk '/iOS Simulator SDKs/{getline; print $NF}')

# Note - We keep the build output in separate directories so that .NET
# bundles iOS with net6.0-ios and Mac Catalyst with net6.0-maccatalyst.
# The lack of symlinks in the ios builds, means we should also be able
# to use the package on Windows with "Pair to Mac".

# Build for iOS and iOS simulator.
echo "::group::Building sentry-cocoa for iOS and iOS simulator"
xcodebuild archive -project Sentry.xcodeproj \
    -scheme Sentry \
    -configuration Release \
    -sdk "$ios_sdk" \
    -archivePath ./Carthage/output-ios.xcarchive \
    SKIP_INSTALL=NO \
    BUILD_LIBRARY_FOR_DISTRIBUTION=YES \
    GCC_PREPROCESSOR_DEFINITIONS='$(inherited) SENTRY_CRASH_MANAGED_RUNTIME=1'
./scripts/remove-architectures.sh ./Carthage/output-ios.xcarchive arm64e
xcodebuild archive -project Sentry.xcodeproj \
    -scheme Sentry \
    -configuration Release \
    -sdk "$ios_simulator_sdk" \
    -archivePath ./Carthage/output-iossimulator.xcarchive \
    SKIP_INSTALL=NO \
    BUILD_LIBRARY_FOR_DISTRIBUTION=YES \
    GCC_PREPROCESSOR_DEFINITIONS='$(inherited) SENTRY_CRASH_MANAGED_RUNTIME=1'
xcodebuild -create-xcframework \
    -framework ./Carthage/output-ios.xcarchive/Products/Library/Frameworks/Sentry.framework \
    -framework ./Carthage/output-iossimulator.xcarchive/Products/Library/Frameworks/Sentry.framework \
    -output ./Carthage/Build-ios/Sentry.xcframework
echo "::endgroup::"

# Separately, build for Mac Catalyst
echo "::group::Building sentry-cocoa for Mac Catalyst"
xcodebuild archive -project Sentry.xcodeproj \
    -scheme Sentry \
    -configuration Release \
    -destination 'generic/platform=macOS,variant=Mac Catalyst' \
    -archivePath ./Carthage/output-maccatalyst.xcarchive \
    SKIP_INSTALL=NO \
    BUILD_LIBRARY_FOR_DISTRIBUTION=YES \
    GCC_PREPROCESSOR_DEFINITIONS='$(inherited) SENTRY_CRASH_MANAGED_RUNTIME=1'
./scripts/remove-architectures.sh ./Carthage/output-maccatalyst.xcarchive arm64e
xcodebuild -create-xcframework \
    -framework ./Carthage/output-maccatalyst.xcarchive/Products/Library/Frameworks/Sentry.framework \
    -output ./Carthage/Build-maccatalyst/Sentry.xcframework
echo "::endgroup::"

# Copy headers - used for generating bindings
mkdir Carthage/Headers
find Carthage/Build-ios/Sentry.xcframework/ios-arm64 -name '*.h' -exec cp {} Carthage/Headers \;

# Remove anything we don't want to bundle in the nuget package.
find Carthage/Build* \( -name Headers -o -name PrivateHeaders -o -name Modules \) -exec rm -rf {} +
rm -rf Carthage/output-*

git rev-parse HEAD > Carthage/.built-from-sha
echo ""

popd >/dev/null
