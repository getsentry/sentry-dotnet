#!/bin/bash

# Run the xcodebuild -showsdks command and capture its output
output=$(xcodebuild -showsdks)

# Check if the command was successful
if [ $? -eq 0 ]; then
    # Use awk to parse the output and extract the SDK information after -sdk
    driverkit_sdk=$(echo "$output" | awk '/DriverKit SDKs/{getline; print $NF}')
    ios_sdk=$(echo "$output" | awk '/iOS SDKs/{getline; print $NF}')
    ios_simulator_sdk=$(echo "$output" | awk '/iOS Simulator SDKs/{getline; print $NF}')
    macos_sdk=$(echo "$output" | awk '/macOS SDKs/{getline; print $NF}')
    tvos_sdk=$(echo "$output" | awk '/tvOS SDKs/{getline; print $NF}')
    tvos_simulator_sdk=$(echo "$output" | awk '/tvOS Simulator SDKs/{getline; print $NF}')
    watchos_sdk=$(echo "$output" | awk '/watchOS SDKs/{getline; print $NF}')
    watchos_simulator_sdk=$(echo "$output" | awk '/watchOS Simulator SDKs/{getline; print $NF}')

    # Print the extracted SDK information
    echo "DriverKit SDK: $driverkit_sdk"
    echo "iOS SDK: $ios_sdk"
    echo "iOS Simulator SDK: $ios_simulator_sdk"
    echo "macOS SDK: $macos_sdk"
    echo "tvOS SDK: $tvos_sdk"
    echo "tvOS Simulator SDK: $tvos_simulator_sdk"
    echo "watchOS SDK: $watchos_sdk"
    echo "watchOS Simulator SDK: $watchos_simulator_sdk"
else
    echo "Error: Failed to run xcodebuild -showsdks command"
fi
