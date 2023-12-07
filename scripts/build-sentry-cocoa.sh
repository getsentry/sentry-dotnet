#!/bin/bash
set -euo pipefail

pushd "$(dirname "$0")" >/dev/null
cd ../modules/sentry-cocoa

rm -rf Carthage

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
    BUILD_LIBRARY_FOR_DISTRIBUTION=YES
xcodebuild archive -project Sentry.xcodeproj \
    -scheme Sentry \
    -configuration Release \
    -sdk "$ios_simulator_sdk" \
    -archivePath ./Carthage/output-iossimulator.xcarchive \
    SKIP_INSTALL=NO \
    BUILD_LIBRARY_FOR_DISTRIBUTION=YES
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
    -destination 'platform=macOS,variant=Mac Catalyst' \
    -archivePath ./Carthage/output-maccatalyst.xcarchive \
    SKIP_INSTALL=NO \
    BUILD_LIBRARY_FOR_DISTRIBUTION=YES
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

cp ../../.git/modules/modules/sentry-cocoa/HEAD Carthage/.built-from-sha
echo ""

popd >/dev/null
