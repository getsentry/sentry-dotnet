cmake_minimum_required(VERSION 3.10)
project(sentrysupplemental LANGUAGES C)

add_library(sentrysupplemental SHARED sentrysupplemental.c)

# Set alignment to 16 KB. See https://developer.android.com/guide/practices/page-sizes#cmake
target_link_options(sentrysupplemental PRIVATE "-Wl,-z,max-page-size=16384")

set(LIBRARY_OUTPUT_PATH ../../bin/${ANDROID_ABI})
