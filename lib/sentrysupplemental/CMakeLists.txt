cmake_minimum_required(VERSION 3.10)
project(sentrysupplemental LANGUAGES C)

add_library(sentrysupplemental SHARED sentrysupplemental.c)

set(LIBRARY_OUTPUT_PATH ../../bin/${ANDROID_ABI})
