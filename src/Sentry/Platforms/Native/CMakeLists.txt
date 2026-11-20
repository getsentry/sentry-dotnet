cmake_minimum_required(VERSION 3.18)
project(sentry-dotnet-native LANGUAGES C CXX)

add_subdirectory(../../../../modules/sentry-native sentry-native)

if(NOT WIN32)
	add_library(sentry-preload OBJECT sentry-preload.c)
	set_target_properties(sentry-preload PROPERTIES POSITION_INDEPENDENT_CODE ON)
	# object files are always linked, even when unreferenced
	install(FILES $<TARGET_OBJECTS:sentry-preload> DESTINATION . COMPONENT sentry-preload RENAME sentry-preload.o)
endif()

if(TARGET sentry-crash)
	target_sources(sentry-crash PRIVATE sentry-crash-transport.c)
	if(TARGET sentry-preload)
		target_link_libraries(sentry-crash PRIVATE sentry-preload)
	endif()
	target_link_libraries(sentry-crash PRIVATE ${CMAKE_DL_LIBS})
endif()
