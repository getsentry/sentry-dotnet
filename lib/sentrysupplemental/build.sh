#!/bin/bash

# this should point at the Android SDK root directory
: ${ANDROID_HOME:=~/Library/Android/sdk}

echo "Starting C Build"
pushd "$(dirname "$0")" > /dev/null

# create the output directories if they don't exist
mkdir -p obj
mkdir -p bin

# remove any existing content
find obj bin -mindepth 1 -delete

# use the latest NDK installed, if not already specified
: ${ANDROID_NDK:=$(dirname $(find $ANDROID_HOME/ndk/*/build -maxdepth 0 | sort -V | tail -1))}  
echo "Using Android NDK at $ANDROID_NDK"

# compile for each ABI
cd obj
basedir=$PWD
for abi in 'armeabi-v7a' 'arm64-v8a' 'x86' 'x86_64';
  do
    echo Building "$abi"
    mkdir -p $basedir/$abi
    cd $basedir/$abi

    # generate build files
    cmake ../.. \
        -DANDROID_ABI=$abi \
        -DANDROID_PLATFORM=21 \
        -DANDROID_NDK=$ANDROID_NDK \
        -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK/build/cmake/android.toolchain.cmake \
        -G Ninja

    # build with Ninja
    ninja -v
  done

popd > /dev/null
echo "C Build Complete"
