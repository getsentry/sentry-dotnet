@echo off
setlocal

rem this should point at JDK 11 (required for Android)
if "%JAVA_HOME_11%" == "" set JAVA_HOME_11=C:\Program Files\Microsoft\jdk-11.0.15.10-hotspot

echo Starting Java Build
echo Using Java SDK at %JAVA_HOME_11%
pushd %~dp0

rem clear and recreate the output directories
if exist obj rmdir /q /s obj
if exist bin rmdir /q /s bin
mkdir obj
mkdir bin

rem compile the Java file(s)
"%JAVA_HOME_11%\bin\javac" -verbose -d ./obj *.java

rem build the Jar
cd obj
"%JAVA_HOME_11%\bin\jar" -cvf ..\bin\sentry-android-supplemental.jar *

popd
echo Java Build Complete
