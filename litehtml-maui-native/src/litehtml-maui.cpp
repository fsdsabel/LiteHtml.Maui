#include "litehtml-maui.h"
#include "litehtml-maui-container.h"

#if _DEBUG && __ANDROID__
#include <android/log.h>

#define LOG(...) __android_log_print(ANDROID_LOG_INFO, "LITEHTML", __VA_ARGS__);
#elif !NDEBUG && __IOS__
#include <asl.h>
#define LOG(...) asl_log(NULL, NULL, ASL_LEVEL_ERR, __VA_ARGS__);
#else
#define LOG(...)
#endif

using namespace std;

std::vector<litehtml::document::ptr> g_documents;

LITEHTML_MAUI_EXPORT HDOCUMENT create_document(maui_container_callbacks callbacks, const char* html, const char* master_css, const char* user_css)
{
	LOG("create document: %s", html);
	auto container = new litehtml::maui::maui_container(callbacks);

	auto document = litehtml::document::createFromString(html, container, master_css, user_css);
	g_documents.push_back(document);
	return document.get();
}

LITEHTML_MAUI_EXPORT void destroy_document(HDOCUMENT document) {
	auto vec_document = std::find_if(g_documents.begin(), g_documents.end(), [document](litehtml::document::ptr p) { return p.get() == document; });
	if (vec_document != g_documents.end()) {
		g_documents.erase(vec_document);
	}	
}


LITEHTML_MAUI_EXPORT maui_size measure_document(HDOCUMENT document, maui_size available_size) {
	LOG("measure_document %i %i", available_size.width, available_size.height);
	if (document != nullptr && document->root() != nullptr) {		
		int max_width = available_size.width < 0 ? INT_MAX : available_size.width;
		int best_width = document->render(max_width);		
		if (best_width < max_width) {
			document->render(best_width);
		}
		auto size = maui_size();
		size.width = document->width();
		size.height = document->height();
		LOG("%i %i", size.width, size.height);
		return size;
	}
	LOG("no document");
	return maui_size{ 0,0 };
}

LITEHTML_MAUI_EXPORT void draw_document(HDOCUMENT document, void* hdc, maui_size size) {
	if (document != nullptr && document->root() != nullptr) {
		document->render(size.width);
		auto pos = position(0, 0, size.width, size.height);
		document->draw((litehtml::uint_ptr)hdc, 0, 0, &pos);		
	}
}

LITEHTML_MAUI_EXPORT bool report_event(HDOCUMENT document, maui_event e, int x, int y, int client_x, int client_y) {
	if (document != nullptr && document->root() != nullptr) {
		LOG("event");
		litehtml::position::vector redraw;
		switch (e)
		{
		case none:
			return false;
		case maui_event::move:
			LOG("over");
			document->on_mouse_over(x, y, client_x, client_y, redraw);
			break;
		case down:
			LOG("down");
			document->on_lbutton_down(x, y, client_x, client_y, redraw);
			break;
		case up:
			LOG("up");
			document->on_lbutton_up(x, y, client_x, client_y, redraw);
			break;
		case leave:
			LOG("leave");
			document->on_mouse_leave(redraw);
			break;
		}
		auto size = (int)redraw.size();
		LOG("%u", size);
		return redraw.size() > 0;	
	}
	return false;
}

LITEHTML_MAUI_EXPORT void test(maui_container_callbacks callbacks) {


}