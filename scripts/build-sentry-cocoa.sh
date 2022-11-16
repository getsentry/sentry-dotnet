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
    SHAFILE=Carthage/Build/.built-from-sha
    [ -f $SHAFILE ] && SHAFROMFILE=$(<$SHAFILE)
    VERSION="$(git describe --tags) ($(git rev-parse --short HEAD))"

    if [ "$SHA" == "$SHAFROMFILE" ]; then
        echo "Sentry Cocoa SDK $VERSION was already built"
    else
        echo "---------- Building Sentry Cocoa SDK $VERSION ---------- "
        rm -rf Carthage
        ../$CARTHAGE build --use-xcframeworks --no-skip-current --platform ios,macCatalyst
        echo $SHA > $SHAFILE
        echo ""
    fi

    cd ..
fi

popd > /dev/null
