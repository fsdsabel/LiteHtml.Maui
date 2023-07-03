# CMakeList.txt : CMake project for litehtml-maui, include source and define
# project specific logic here.
#
cmake_minimum_required (VERSION 3.19)

set(CMAKE_XCODE_ATTRIBUTE_CODE_SIGNING_REQUIRED "NO")
set(CMAKE_XCODE_ATTRIBUTE_CODE_SIGN_IDENTITY "")

set(BUILD_TESTING OFF CACHE INTERNAL "Turn off tests") 

include(FetchContent)

project ("litehtml-maui")

FetchContent_Declare(
	litehtml
	GIT_REPOSITORY https://github.com/fsdsabel/litehtml.git
	
)

FetchContent_MakeAvailable(litehtml)

# Build static library for iOS
if(${CMAKE_SYSTEM_NAME} MATCHES "iOS")
    set(LIB_TYPE STATIC)
    ADD_DEFINITIONS(-D__IOS__)
else()
    set(LIB_TYPE SHARED)
endif()


# Add source to this project's executable.


add_library (litehtml-maui ${LIB_TYPE} "src/litehtml-maui.cpp" "src/litehtml-maui.h" "src/litehtml-maui-container.h" "src/litehtml-maui-container.cpp")

set_target_properties(litehtml-maui PROPERTIES
    CXX_STANDARD 11
    C_STANDARD 99
    PUBLIC_HEADER "src/litehtml-maui.h" 
)

target_link_libraries(litehtml-maui litehtml)


install(TARGETS litehtml-maui DESTINATION lib)

# TODO: Add tests and install targets if needed.
