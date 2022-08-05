@echo off
setlocal

rem this should point at the Android SDK root directory
if "%ANDROID_HOME%" == "" set ANDROID_HOME=%LOCALAPPDATA%\Android\Sdk

echo Starting C Build
pushd %~dp0

rem clear and recreate the output directories
if exist obj rmdir /q /s obj
if exist bin rmdir /q /s bin
mkdir obj
mkdir bin

rem use the latest NDK installed, if not already specified
if "%ANDROID_NDK%" == "" for /f %%i in ('dir %LOCALAPPDATA%\Android\Sdk\ndk /b /a:d /o:n') do (
    set ANDROID_NDK=%LOCALAPPDATA%\Android\Sdk\ndk\%%i
)
echo Using Android NDK at %ANDROID_NDK%

rem compile for each ABI
cd obj
set basedir=%cd%
for %%i in (armeabi-v7a,arm64-v8a,x86,x86_64) do (
    echo Building %%i

    mkdir %basedir%\%%i
    cd %basedir%\%%i

    rem generate build files
    cmake ..\.. ^
        -DANDROID_ABI=%%i ^
        -DANDROID_PLATFORM=21 ^
        -DANDROID_NDK=%ANDROID_NDK% ^
        -DCMAKE_TOOLCHAIN_FILE=%ANDROID_NDK%\build\cmake\android.toolchain.cmake ^
        -G Ninja

    rem build with Ninja
    ninja -v
)

popd
echo C Build Complete
