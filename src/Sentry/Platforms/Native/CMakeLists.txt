# Builds sentry-native with the preload object. Configure as
# scripts/build-sentry-native.ps1 does.
cmake_minimum_required(VERSION 3.18)
project(sentry-dotnet-native LANGUAGES C CXX)

add_subdirectory(../../../../modules/sentry-native sentry-native)

if(NOT WIN32)
	# installs the crash handlers under the .NET runtime's
	add_library(sentry-preload OBJECT sentry-preload.c)
	set_target_properties(sentry-preload PROPERTIES POSITION_INDEPENDENT_CODE ON)
	# an object file, because nothing would pull its constructor out of an archive
	install(FILES $<TARGET_OBJECTS:sentry-preload> DESTINATION . COMPONENT sentry-preload RENAME sentry-preload.o)
endif()
