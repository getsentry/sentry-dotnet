mkdir -p ../obj/cmake
pushd ../obj/cmake

for abi in 'armeabi-v7a' 'arm64-v8a' 'x86' 'x86_64';
  do
    echo Building "$abi"
    cmake ../../C \
        -DANDROID_ABI=$abi \
        -DANDROID_PLATFORM=android-30 \
        -DANDROID_NDK=$ANDROID_HOME/ndk/21.4.7075529 \
        -DCMAKE_TOOLCHAIN_FILE=$ANDROID_HOME/ndk/21.4.7075529/build/cmake/android.toolchain.cmake \
        -G Ninja
    ninja
    mkdir -p ../../C/$abi
    cp tmp/*.so ../../C/$abi
    rm tmp/*.so
  done
  