set ANDROID_SDK=C:\Program Files (x86)\Android\android-sdk\
mkdir %~dp0\..\out\_buildandroid
cmake -S . -G Ninja  -B ..\out\_buildandroid\arm64-v8a -DANDROID_ABI=arm64-v8a -DANDROID_PLATFORM=android-21 -DANDROID_NDK="c:\Program Files (x86)\Android\android-sdk\ndk-bundle" -DCMAKE_TOOLCHAIN_FILE="C:\Program Files (x86)\Android\android-sdk\ndk-bundle\build\cmake\android.toolchain.cmake"  -DCMAKE_MAKE_PROGRAM="%~dp0\tools\ninja.exe" -DCMAKE_BUILD_TYPE=Release -DCMAKE_CXX_FLAGS_RELEASE=-g0  -DBUILD_TESTING=OFF
cmake --build ../out/_buildandroid\arm64-v8a --config Release 
"%ANDROID_SDK%\ndk-bundle\toolchains\llvm\prebuilt\windows-x86_64\aarch64-linux-android\bin\strip.exe" ..\out\_buildandroid\arm64-v8a\liblitehtml-maui.so

cmake -S . -G Ninja  -B ..\out\_buildandroid\x86 -DANDROID_ABI=x86 -DANDROID_PLATFORM=android-21 -DANDROID_NDK="c:\Program Files (x86)\Android\android-sdk\ndk-bundle" -DCMAKE_TOOLCHAIN_FILE="C:\Program Files (x86)\Android\android-sdk\ndk-bundle\build\cmake\android.toolchain.cmake"  -DCMAKE_MAKE_PROGRAM="%~dp0\tools\ninja.exe" -DCMAKE_BUILD_TYPE=Release -DCMAKE_CXX_FLAGS_RELEASE=-g0  -DBUILD_TESTING=OFF
cmake --build ../out/_buildandroid\x86 --config Release 
"%ANDROID_SDK%\ndk-bundle\toolchains\llvm\prebuilt\windows-x86_64\i686-linux-android\bin\strip.exe" ..\out\_buildandroid\x86\liblitehtml-maui.so

cmake -S . -G Ninja  -B ..\out\_buildandroid\x86_64 -DANDROID_ABI=x86_64 -DANDROID_PLATFORM=android-21 -DANDROID_NDK="c:\Program Files (x86)\Android\android-sdk\ndk-bundle" -DCMAKE_TOOLCHAIN_FILE="C:\Program Files (x86)\Android\android-sdk\ndk-bundle\build\cmake\android.toolchain.cmake"  -DCMAKE_MAKE_PROGRAM="%~dp0\tools\ninja.exe" -DCMAKE_BUILD_TYPE=Release -DCMAKE_CXX_FLAGS_RELEASE=-g0  -DBUILD_TESTING=OFF
cmake --build ../out/_buildandroid\x86_64 --config Release 
"%ANDROID_SDK%\ndk-bundle\toolchains\llvm\prebuilt\windows-x86_64\i686-linux-android\bin\strip.exe" ..\out\_buildandroid\x86_64\liblitehtml-maui.so

mkdir ..\LiteHtmlMaui\runtimes\android-arm64\native\
mkdir ..\LiteHtmlMaui\runtimes\android-x86\native\
mkdir ..\LiteHtmlMaui\runtimes\android-x86_64\native\
copy /Y ..\out\_buildandroid\arm64-v8a\liblitehtml-maui.so ..\LiteHtmlMaui\runtimes\android-arm64\native\
copy /Y ..\out\_buildandroid\x86\liblitehtml-maui.so ..\LiteHtmlMaui\runtimes\android-x86\native\
copy /Y ..\out\_buildandroid\x86_64\liblitehtml-maui.so ..\LiteHtmlMaui\runtimes\android-x86_64\native\