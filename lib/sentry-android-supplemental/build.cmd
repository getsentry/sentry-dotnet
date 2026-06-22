@echo off
setlocal

rem this should point at JDK 21 (required for Android)
if "%JAVA_HOME_21%" == "" set JAVA_HOME_21=C:\Program Files\Eclipse Adoptium\jdk-21

echo Starting Java Build
echo Using Java SDK at %JAVA_HOME_21%
pushd %~dp0

rem clear and recreate the output directories
if exist obj rmdir /q /s obj
if exist bin rmdir /q /s bin
mkdir obj
mkdir bin

rem compile the Java file(s)
"%JAVA_HOME_21%\bin\javac" -verbose -d ./obj *.java

rem build the Jar
cd obj
"%JAVA_HOME_21%\bin\jar" -cvf ..\bin\sentry-android-supplemental.jar *

popd
echo Java Build Complete
