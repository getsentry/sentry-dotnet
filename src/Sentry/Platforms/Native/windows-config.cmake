# Include debug info in the static library itself. See https://github.com/getsentry/sentry-native/issues/895 for context.
set(_sentry_msvc_flags "/Z7 /O2 /Ob1 /DNDEBUG")

# Build with Control Flow Guard so a Native AOT consumer (ControlFlowGuard=Guard) doesn't hit LNK4291.
# See https://github.com/getsentry/sentry-dotnet/pull/5298 for context. /guard:ehcont is x64-only.
string(APPEND _sentry_msvc_flags " /guard:cf")
if("$ENV{PROCESSOR_ARCHITECTURE}" STREQUAL "AMD64")
  string(APPEND _sentry_msvc_flags " /guard:ehcont")
endif()

set(CMAKE_C_FLAGS_RELWITHDEBINFO "${_sentry_msvc_flags}" CACHE STRING "C Flags for RelWithDebInfo" FORCE)
set(CMAKE_CXX_FLAGS_RELWITHDEBINFO "${_sentry_msvc_flags}" CACHE STRING "CXX Flags for RelWithDebInfo" FORCE)
