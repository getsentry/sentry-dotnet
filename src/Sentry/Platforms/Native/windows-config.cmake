# Include debug info in the static library itself. See https://github.com/getsentry/sentry-native/issues/895 for context.
set(CMAKE_C_FLAGS_RELWITHDEBINFO "/Z7 /O2 /Ob1 /DNDEBUG" CACHE STRING "C Flags for RelWithDebInfo" FORCE)
set(CMAKE_CXX_FLAGS_RELWITHDEBINFO "/Z7 /O2 /Ob1 /DNDEBUG" CACHE STRING "CXX Flags for RelWithDebInfo" FORCE)