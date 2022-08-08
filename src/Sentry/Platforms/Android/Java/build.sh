#!/bin/bash

# this should point at JDK 11 (required for Android)
: ${JAVA_HOME_11:=/Library/Java/JavaVirtualMachines/microsoft-11.jdk/Contents/Home}

echo "Starting Java Build"
echo "Using Java SDK at $JAVA_HOME_11"
pushd "$(dirname "$0")" > /dev/null

# create the output directories if they don't exist
mkdir -p obj
mkdir -p bin

# remove any existing content
find obj bin -mindepth 1 -delete

# compile the Java file(s)
"$JAVA_HOME_11/bin/javac" -verbose -d ./obj *.java

# build the Jar
cd obj
"$JAVA_HOME_11/bin/jar" -cvf ../bin/sentry-android-supplemental.jar *

popd > /dev/null
echo "Java Build Complete"
