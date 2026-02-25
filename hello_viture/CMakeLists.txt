cmake_minimum_required(VERSION 3.10)
project(hello_viture)

# Set path to Viture SDK
set(VITURE_SDK_DIR "${CMAKE_SOURCE_DIR}/../Viture/Linux_x86_64/release")

# Include directories
include_directories(${VITURE_SDK_DIR}/include)

# Library directories
link_directories(${VITURE_SDK_DIR}/x86_64)

# Find necessary libraries
# Note: libglasses.so depends on other libs in the same folder, so we might need to set RPATH
add_executable(hello_viture main.cpp)

# Link against libglasses
target_link_libraries(hello_viture glasses)

# Set RPATH to find the shared libraries at runtime relative to the executable
# or just assume the user sets LD_LIBRARY_PATH.
# But for convenience, let's set RPATH to the absolute path of the lib dir.
set_target_properties(hello_viture PROPERTIES
    BUILD_RPATH "${VITURE_SDK_DIR}/x86_64"
    INSTALL_RPATH "${VITURE_SDK_DIR}/x86_64"
)
