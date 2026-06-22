# Include debug info in the static library itself. See https://github.com/getsentry/sentry-native/issues/895 for context.
set(_sentry_msvc_flags "/Z7 /O2 /Ob1 /DNDEBUG")

# Build the static lib with Control Flow Guard metadata so a Native AOT consumer that links it
# with <ControlFlowGuard>Guard</ControlFlowGuard> doesn't get LNK4291 against every __try/__except
# translation unit ("module may contain '__except' but was not compiled with /guard:ehcont").
# See https://github.com/getsentry/sentry-dotnet/issues/4801.
# /guard:cf applies to both x64 and arm64; /guard:ehcont (EH continuation metadata) is x64-only,
# and LNK4291 itself is an x64-only warning, so only emit ehcont for the x64 build.
string(APPEND _sentry_msvc_flags " /guard:cf")
if("$ENV{PROCESSOR_ARCHITECTURE}" STREQUAL "AMD64")
  string(APPEND _sentry_msvc_flags " /guard:ehcont")
endif()

set(CMAKE_C_FLAGS_RELWITHDEBINFO "${_sentry_msvc_flags}" CACHE STRING "C Flags for RelWithDebInfo" FORCE)
set(CMAKE_CXX_FLAGS_RELWITHDEBINFO "${_sentry_msvc_flags}" CACHE STRING "CXX Flags for RelWithDebInfo" FORCE)
