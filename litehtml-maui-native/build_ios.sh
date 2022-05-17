#/bin/zsh

#cmake -S. -G Xcode -B../out/_buildios \
#    -DCMAKE_SYSTEM_NAME=iOS \
#	"-DCMAKE_OSX_ARCHITECTURES=arm64;x86_64" \
#	-DCMAKE_XCODE_ATTRIBUTE_ONLY_ACTIVE_ARCH=NO \
#	-DCMAKE_IOS_INSTALL_COMBINED=YES \
 #   -DCMAKE_Swift_COMPILER_FORCED=true \
  #  -DCMAKE_OSX_DEPLOYMENT_TARGET=11.0 \
#	-DCMAKE_INSTALL_PREFIX=`pwd`/../out/ios

#cmake --build ../out/_buildios --config Release --target install -- -UseModernBuildSystem=NO

cmake -S. -G Xcode -B../out/_buildios -DCMAKE_TOOLCHAIN_FILE=ios.toolchain.cmake -DPLATFORM=OS64COMBINED -DCMAKE_INSTALL_PREFIX=`pwd`/../out/ios -DBUILD_TESTING=OFF
cmake --build ../out/_buildios --config Release
cmake --install ../out/_buildios --config Release

#cmake --build ../out/_buildios --config Debug
#cmake --install ../out/_buildios --config Debug


cp ../out/ios/lib/*.a ../LiteHtmlMaui/Platforms/iOS/NativeLibs/
