mkdir %~dp0\..\out\_buildwindows
cmake -S . -G "Visual Studio 17 2022" -A x64 -B ..\out\_buildwindows\x64 -DCMAKE_BUILD_TYPE=Release -DBUILD_TESTING=OFF

cmake --build ../out/_buildwindows\x64 --config Release

mkdir ..\LiteHtmlMaui\runtimes\win-x64\native\
copy /Y ..\out\_buildwindows\x64\Release\litehtml-maui.dll ..\LiteHtmlMaui\runtimes\win-x64\native\