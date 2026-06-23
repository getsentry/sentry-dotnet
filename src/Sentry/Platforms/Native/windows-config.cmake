# Include debug info in the static library itself. See https://github.com/getsentry/sentry-native/issues/895 for context.
# SENTRY_EXTRA_MSVC_FLAGS is set by scripts/build-sentry-native.ps1 with arch-aware CFG/EHCONT flags.
# See https://github.com/getsentry/sentry-dotnet/pull/5298 for context.
set(CMAKE_C_FLAGS_RELWITHDEBINFO "/Z7 /O2 /Ob1 /DNDEBUG ${SENTRY_EXTRA_MSVC_FLAGS}" CACHE STRING "C Flags for RelWithDebInfo" FORCE)
set(CMAKE_CXX_FLAGS_RELWITHDEBINFO "/Z7 /O2 /Ob1 /DNDEBUG ${SENTRY_EXTRA_MSVC_FLAGS}" CACHE STRING "CXX Flags for RelWithDebInfo" FORCE)
