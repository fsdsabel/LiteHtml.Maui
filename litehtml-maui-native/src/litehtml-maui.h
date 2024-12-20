#pragma once

#include <iostream>
#include <litehtml.h>

// ANDROID:
// cmake -G Ninja -H..\..\.. -Barm64-v8a -DANDROID_ABI=arm64-v8a -DANDROID_PLATFORM=android-21 -DANDROID_NDK="c:\Program Files (x86)\Android\android-sdk\ndk-bundle" -DCMAKE_TOOLCHAIN_FILE="C:\Program Files (x86)\Android\android-sdk\ndk-bundle\build\cmake\android.toolchain.cmake"  -DCMAKE_MAKE_PROGRAM="E:\Projekte_NoBackup\litehtml-maui\src\litehtml-maui-native\out\build\ninja.exe"
// stripped debug symbols:
// cmake -G Ninja  -H..\..\.. -Barm64-v8a -DANDROID_ABI=arm64-v8a -DANDROID_PLATFORM=android-21 -DANDROID_NDK="c:\Program Files (x86)\Android\android-sdk\ndk-bundle" -DCMAKE_TOOLCHAIN_FILE="C:\Program Files (x86)\Android\android-sdk\ndk-bundle\build\cmake\android.toolchain.cmake"  -DCMAKE_MAKE_PROGRAM="E:\Projekte_NoBackup\litehtml-maui\src\litehtml-maui-native\out\build\ninja.exe" -DCMAKE_BUILD_TYPE=Release -DCMAKE_CXX_FLAGS_RELEASE=-g0
// "C:\Program Files (x86)\Android\android-sdk\ndk-bundle\toolchains\llvm\prebuilt\windows-x86_64\aarch64-linux-android\bin\strip.exe" liblitehtml-maui.so

// IOS: cmake -S. -G Xcode -B_buildios \
    -DCMAKE_SYSTEM_NAME=iOS \
	"-DCMAKE_OSX_ARCHITECTURES=arm64;x86_64" \
	-DCMAKE_XCODE_ATTRIBUTE_ONLY_ACTIVE_ARCH=NO \
	-DCMAKE_IOS_INSTALL_COMBINED=YES \
    -DCMAKE_Swift_COMPILER_FORCED=true \
    -DCMAKE_OSX_DEPLOYMENT_TARGET=11.0 \
	-DCMAKE_INSTALL_PREFIX=`pwd`/_install

// cmake --build ./_buildios --config Release --target install
// cmake --build ./_buildios -- -sdk iphonesimulator
// cmake --build ./_buildios -- -sdk iphoneos


#if defined (_WIN32)
#if defined(litehtml_maui_EXPORTS)
#define  LITEHTML_MAUI_EXPORT __declspec(dllexport)
#else
#define  LITEHTML_MAUI_EXPORT __declspec(dllimport)
#endif /* MyLibrary_EXPORTS */
#else /* defined (_WIN32) */
#define LITEHTML_MAUI_EXPORT
#endif

extern "C" {

#define HDOCUMENT litehtml::document*

	struct font_desc{
		const char * faceName;
		int size;
		int weight;
		litehtml::font_style italic;
		unsigned int decoration;
		litehtml::font_metrics* fm;
	};


	struct maui_size {
		int width;
		int height;
	};

	struct defaults {
		defaults() {
			font_size = 0;
			font_face_name = nullptr;
			language = nullptr;
			culture = nullptr;
		}
		int font_size;
		char* font_face_name;
		char* language;
		char* culture;
	};

	struct maui_background_paint
	{
		const char*					image;
		const char*					baseurl;
		litehtml::background_attachment	attachment;
		litehtml::background_repeat		repeat;
		litehtml::web_color				color;
		litehtml::position				clip_box;
		litehtml::position				origin_box;
		litehtml::position				border_box;
		litehtml::border_radiuses			border_radius;
		maui_size				image_size;
		int						position_x;
		int						position_y;
		bool					is_root;

		maui_background_paint(const litehtml::background_layer& val) {			
			image = nullptr;
			baseurl = nullptr;
			image_size = maui_size();
			position_x = 0;
			position_y = 0;
			attachment = val.attachment;
			repeat = val.repeat;
			clip_box = val.clip_box;
			origin_box = val.origin_box;
			border_box = val.border_box;
			border_radius = val.border_radius;
			is_root = val.is_root;
		}
	};

	struct maui_list_marker
	{
		const char*	image;
		const char*	baseurl;
		litehtml::list_style_type	marker_type;
		litehtml::web_color		color;
		litehtml::position		pos;
		int				index;
		litehtml::uint_ptr		font;
	};


	typedef int (*text_width)(const char* text, font_desc* hFont);
	typedef litehtml::position(*get_client_rect)();
	typedef void(*draw_text)(litehtml::uint_ptr hdc, const char* text, font_desc* hFont, litehtml::web_color color, const litehtml::position& pos);
	typedef void(*fill_font_metrics)(font_desc* hFont, litehtml::font_metrics* fm);
	typedef void(*draw_background)(const maui_background_paint& bg);
	typedef void(*set_cursor)(const char* cursor);
	typedef void(*draw_borders)(const litehtml::borders& borders, const litehtml::position& draw_pos, bool root);
	typedef void(*load_image)(const char* src, const char* baseurl, bool redraw_on_ready);
	typedef void(*get_image_size)(const char* src, const char* baseurl, maui_size* size);
	typedef void(*draw_list_marker)(const maui_list_marker& marker, font_desc* hFont);
	typedef void(*get_defaults)(defaults& defaults);
	typedef void(*on_anchor_click)(const char* url);
	typedef int(*pt_to_px)(int pt);
    typedef void(*import_css)(char** text, const char* url, char** baseurl);
    typedef void(*free_string)(char* str);


	struct maui_container_callbacks {

		text_width text_width;
		get_client_rect get_client_rect;
		draw_text draw_text;
		fill_font_metrics fill_font_metrics;
		draw_background draw_background;
		set_cursor set_cursor;
		load_image load_image;
		get_image_size get_image_size;
		draw_borders draw_borders;
		draw_list_marker draw_list_marker;
		get_defaults get_defaults;
		on_anchor_click on_anchor_click;
		pt_to_px pt_to_px;
        import_css import_css;
		free_string free_string;
	};

	enum maui_event {
		none,
		move,
		down,
		up,
		leave
	};


	LITEHTML_MAUI_EXPORT HDOCUMENT create_document(maui_container_callbacks callbacks, const char* html, const char* master_css, const char* user_css);

	LITEHTML_MAUI_EXPORT void destroy_document(HDOCUMENT document);

	LITEHTML_MAUI_EXPORT maui_size measure_document(HDOCUMENT document, maui_size available_size);

	LITEHTML_MAUI_EXPORT void draw_document(HDOCUMENT document, void* hdc, maui_size size);

	LITEHTML_MAUI_EXPORT bool report_event(HDOCUMENT document, maui_event e, int x, int y, int client_x, int client_y);
}
