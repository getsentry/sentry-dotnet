#!/bin/bash
set -euo pipefail

pushd "$(dirname "$0")" >/dev/null
cd ../modules/sentry-cocoa

rm -rf Carthage

# Grabbing the first SDK versions
sdks=$(xcodebuild -showsdks)
ios_sdk=$(echo "$sdks" | awk '/iOS SDKs/{getline; print $NF}')
ios_simulator_sdk=$(echo "$sdks" | awk '/iOS Simulator SDKs/{getline; print $NF}')
macos_sdk=$(echo "$sdks" | awk '/macOS SDKs/{getline; print $NF}')

# Note - We keep the build output in separate directories so that .NET
# bundles iOS with net6.0-ios and Mac Catalyst with net6.0-maccatalyst.
# The lack of symlinks in the ios builds, means we should also be able
# to use the package on Windows with "Pair to Mac".

# Build for iOS and iOS simulator.
xcodebuild -project Sentry.xcodeproj \
    -scheme Sentry \
    -configuration Release \
    -sdk "$ios_sdk" \
    -derivedDataPath ./Carthage/output-ios
xcodebuild -project Sentry.xcodeproj \
    -scheme Sentry \
    -configuration Release \
    -sdk "$ios_simulator_sdk" \
    -derivedDataPath ./Carthage/output-ios
xcodebuild -create-xcframework \
    -framework ./Carthage/output-ios/Build/Products/Release-iphoneos/Sentry.framework \
    -framework ./Carthage/output-ios/Build/Products/Release-iphonesimulator/Sentry.framework \
    -output ./Carthage/Build-ios/Sentry.xcframework

# Build for macOS.
xcodebuild -project Sentry.xcodeproj \
    -scheme Sentry \
    -configuration Release \
    -sdk "$macos_sdk" \
    -derivedDataPath ./Carthage/output-macos
xcodebuild -create-xcframework \
    -framework ./Carthage/output-macos/Build/Products/Release/Sentry.framework \
    -output ./Carthage/Build-macos/Sentry.xcframework

# Separately, build for Mac Catalyst
xcodebuild -project Sentry.xcodeproj \
    -scheme Sentry \
    -configuration Release \
    -destination 'platform=macOS,variant=Mac Catalyst' \
    -derivedDataPath ./Carthage/output-maccatalyst
xcodebuild -create-xcframework \
    -framework ./Carthage/output-maccatalyst/Build/Products/Release-maccatalyst/Sentry.framework \
    -output ./Carthage/Build-maccatalyst/Sentry.xcframework

# Copy headers - used for generating bindings
mkdir Carthage/Headers
find Carthage/Build-ios/Sentry.xcframework/ios-arm64 -name '*.h' -exec cp {} Carthage/Headers \;

# Remove anything we don't want to bundle in the nuget package.
find Carthage/Build* \( -name Headers -o -name PrivateHeaders -o -name Modules \) -exec rm -rf {} +
rm -rf Carthage/output-*

cp ../../.git/modules/modules/sentry-cocoa/HEAD Carthage/.built-from-sha
echo ""

popd >/dev/null
