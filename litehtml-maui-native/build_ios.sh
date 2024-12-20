#/bin/zsh

# iPhoneOS
cmake -S. -G Xcode -B../out/_buildios -DCMAKE_TOOLCHAIN_FILE=ios.toolchain.cmake -DPLATFORM=OS64 -DCMAKE_INSTALL_PREFIX=`pwd`/../out/ios -DBUILD_TESTING=OFF -DDEPLOYMENT_TARGET=13.0
cmake --build ../out/_buildios --config Release
cmake --install ../out/_buildios --config Release

# iPhoneOS Simulator ARM 64
cmake -S. -G Xcode -B../out/_buildiossimarm -DCMAKE_TOOLCHAIN_FILE=ios.toolchain.cmake -DPLATFORM=SIMULATORARM64 -DCMAKE_INSTALL_PREFIX=`pwd`/../out/iossimarm -DBUILD_TESTING=OFF -DDEPLOYMENT_TARGET=13.0
cmake --build ../out/_buildiossimarm --config Release
cmake --install ../out/_buildiossimarm --config Release

# iPhoneOS Simulator x64
cmake -S. -G Xcode -B../out/_buildiossim64 -DCMAKE_TOOLCHAIN_FILE=ios.toolchain.cmake -DPLATFORM=SIMULATOR64 -DCMAKE_INSTALL_PREFIX=`pwd`/../out/iossim64 -DBUILD_TESTING=OFF -DDEPLOYMENT_TARGET=13.0
cmake --build ../out/_buildiossim64 --config Release
cmake --install ../out/_buildiossim64 --config Release

# create fat lib for simulator
mkdir -p ../out/iossim
lipo -create ../out/iossimarm/lib/libgumbo.a ../out/iossim64/lib/libgumbo.a -output ../out/iossim/libgumbo_sim_universal.a
lipo -create ../out/iossimarm/lib/liblitehtml.a ../out/iossim64/lib/liblitehtml.a -output ../out/iossim/liblitehtml_sim_universal.a
lipo -create ../out/iossimarm/lib/liblitehtml-maui.a ../out/iossim64/lib/liblitehtml-maui.a -output ../out/iossim/liblitehtml-maui_sim_universal.a

# combine libraries per sdk
libtool -static -o ../out/ios/liblitehtml-maui-pkg.a ../out/ios/lib/libgumbo.a ../out/ios/lib/liblitehtml.a ../out/ios/lib/liblitehtml-maui.a
libtool -static -o ../out/iossim/liblitehtml-maui-pkg_sim.a ../out/iossim/libgumbo_sim_universal.a ../out/iossim/liblitehtml_sim_universal.a ../out/iossim/liblitehtml-maui_sim_universal.a

# create XCFramework


xcodebuild -create-xcframework \
           -library ../out/ios/liblitehtml-maui-pkg.a \
           -library ../out/iossim/liblitehtml-maui-pkg_sim.a \
           -output ../LiteHtmlMaui/Platforms/iOS/NativeLibs/LiteHtmlMaui.xcframework