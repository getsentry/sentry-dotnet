#!/bin/bash

dotnet build -c Release

for sampleDll in bin/Release/*/*Console.dll; do
    [ -e "$sampleDll" ] || continue
    printf "\nRunning: $sampleDll\n"
    dotnet $sampleDll
done
for sampleExe in bin/Release/*/*Console.exe; do
    [ -e "$sampleExe" ] || continue
    printf "\nRunning: $sampleExe\n"
    mono $sampleExe
done
