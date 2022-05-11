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


HCONTEXT create_context() {
	return new litehtml::context();
}

void destroy_context(HCONTEXT context)
{
	delete context;
}

void load_master_stylesheet(HCONTEXT context, const litehtml::tchar_t* css)
{
	context->load_master_stylesheet(css);
}

LITEHTML_MAUI_EXPORT HDOCUMENT create_document(HCONTEXT context, maui_container_callbacks callbacks, const litehtml::tchar_t* html, const litehtml::tchar_t* user_css)
{
	LOG("create document: %s", html);
	auto container = new litehtml::maui::maui_container(callbacks);
	/*
	auto pos = callbacks.get_client_rect();
	auto p=callbacks.pt_to_px(199);
	
	defaults d;		 
	callbacks.get_defaults(d);
	callbacks.draw_text(0, d.font_face_name, nullptr, litehtml::web_color(1, 2, 3, p), pos);*/
	/*
	background_paint bp;
	bp.baseurl = _t("hellp");
	callbacks.draw_background(bp);
	*//*
	font_desc fd;
	fd.faceName = _t("face");
	font_metrics fm;
	fd.fm = &fm;
	callbacks.fill_font_metrics(&fd, &fm);

	auto tw = callbacks.text_width(_t("some text"), &fd);

	callbacks.pt_to_px(tw);
	*/
	css cssuser;	
	const litehtml::tstring empty = _t("");
	std::shared_ptr<document> tempdoc = document::createFromString(empty.c_str(), container, context);
	std::shared_ptr<media_query_list> mlist;
	cssuser.parse_stylesheet(user_css, empty.c_str(), tempdoc, mlist);
	auto document = litehtml::document::createFromString(html, container, context, &cssuser);
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