#!/bin/bash

pushd "$(dirname "$0")" > /dev/null
cd ../modules

# We need a custom build of Carthage that supports building XCFrameworks for Mac Catalyst,
# so we'll bring it in as a submodule and build it here.
# See https://github.com/getsentry/sentry-cocoa/issues/2031
# and https://github.com/Carthage/Carthage/pull/3235

CARTHAGE=Carthage/.build/debug/carthage

if [ ! -f $CARTHAGE ]; then
    cd Carthage
    echo "---------- Building Carthage ---------- "
    make all
    echo ""
    cd ..
fi

if [ $? -eq 0 ]
then
    cd sentry-cocoa

    # Keep track of the submodule's SHA so we only build when we need to
    SHA=$(git rev-parse HEAD)
    SHAFILE=Carthage/.built-from-sha
    [ -f $SHAFILE ] && SHAFROMFILE=$(<$SHAFILE)
    VERSION="$(git describe --tags) ($(git rev-parse --short HEAD))"

    if [ "$SHA" == "$SHAFROMFILE" ]; then
        echo "Sentry Cocoa SDK $VERSION was already built"
    else
        echo "---------- Building Sentry Cocoa SDK $VERSION ---------- "
        rm -rf Carthage

        # Delete SentryPrivate and SentrySwiftUI schemes
        # we dont want to build them

        rm Sentry.xcodeproj/xcshareddata/xcschemes/SentryPrivate.xcscheme
        rm Sentry.xcodeproj/xcshareddata/xcschemes/SentrySwiftUI.xcscheme

        # Note - We keep the build output in separate directories so that .NET
        # bundles iOS with net6.0-ios and Mac Catalyst with net6.0-maccatalyst.
        # The lack of symlinks in the ios builds, means we should also be able
        # to use the package on Windows with "Pair to Mac".

        # Build for iOS.  We'll get both ios and ios-simulator from this.
        ../$CARTHAGE build --use-xcframeworks --no-skip-current --platform ios
        mv Carthage/Build Carthage/Build-ios

        # Separately, build for Mac Catalyst in its own directory.
        ../$CARTHAGE build --use-xcframeworks --no-skip-current --platform macCatalyst
        mv Carthage/Build Carthage/Build-maccatalyst

        # Copy headers - used for generating bindings
        mkdir Carthage/Headers
        find Carthage/Build-ios/Sentry.xcframework/ios-arm64 -name '*.h' -exec cp {} Carthage/Headers \;

        echo $SHA > $SHAFILE
        echo ""
    fi

    # Remove anything we don't want to bundle in the nuget package.
    find Carthage/Build* \( -name Headers -o -name PrivateHeaders -o -name Modules \) -exec rm -rf {} +
fi

popd > /dev/null
