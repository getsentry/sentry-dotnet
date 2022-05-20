cmake_minimum_required(VERSION 3.10)
project(buggy LANGUAGES C)

add_library(buggy SHARED buggy.c)

set(LIBRARY_OUTPUT_PATH tmp)
