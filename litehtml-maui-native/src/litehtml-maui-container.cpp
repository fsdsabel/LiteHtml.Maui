#include <litehtml.h>
#include <cwctype>
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

namespace litehtml
{
	namespace maui {
		char* dupstr(char* str) {
			if (str == nullptr) return nullptr;

			return (char*)strdup(str);
		}

		litehtml::uint_ptr maui_container::create_font(const char* faceName, int size, int weight, litehtml::font_style italic, unsigned int decoration, litehtml::font_metrics* fm)
		{
			LOG("create_font");
			font_desc* fd = new font_desc();
			fd->faceName = faceName;
			fd->size = size;
			fd->weight = weight;
			fd->italic = italic;
			fd->decoration = decoration;
			// fd->fm = fm; // TODO, may be invalid - but we don't really need that
            fd->fm = nullptr;
            if (fm) {
				_callbacks.fill_font_metrics(fd, fm);
			}
            _font_handle_index++;
			_fonts.insert(std::pair<litehtml::uint_ptr, font_desc*>(_font_handle_index, fd));
			return _font_handle_index;
		}
		void maui_container::delete_font(litehtml::uint_ptr hFont)
		{			
			font_desc* fd = _fonts.at(hFont);
			delete fd;
            _fonts.erase(hFont);
		}
		int maui_container::text_width(const char* text, litehtml::uint_ptr hFont)
		{
			LOG("text_width");
			return _callbacks.text_width(text, _fonts.at(hFont));
		}
		void maui_container::draw_text(litehtml::uint_ptr hdc, const char* text, litehtml::uint_ptr hFont, litehtml::web_color color, const litehtml::position& pos)
		{
			LOG("draw_text");
			_callbacks.draw_text(hdc, text, _fonts.at(hFont), color, pos);
		}
		int maui_container::pt_to_px(int pt) const
		{
			LOG("pt_to_px");
			return _callbacks.pt_to_px(pt);
		}
		int maui_container::get_default_font_size() const
		{
			defaults def;
			_callbacks.get_defaults(def);
			return def.font_size;
		}
		const char* maui_container::get_default_font_name() const
		{
			LOG("get_default_font_name");
			defaults def;
			_callbacks.get_defaults(def);			
			return dupstr(def.font_face_name);
		}
		void maui_container::draw_list_marker(litehtml::uint_ptr hdc, const litehtml::list_marker& marker)
		{
			LOG("draw_list_marker");
			const maui_list_marker maui_marker = {
				marker.image.c_str(),
				marker.baseurl,
				marker.marker_type,
				marker.color,
				marker.pos,
				marker.index,
				marker.font
			};

			_callbacks.draw_list_marker(maui_marker, _fonts.at(marker.font));
		}
		void maui_container::load_image(const char* src, const char* baseurl, bool redraw_on_ready)
		{
			LOG("load_image");
			_callbacks.load_image(src, baseurl, redraw_on_ready);
		}
		void maui_container::get_image_size(const char* src, const char* baseurl, litehtml::size& sz)
		{
			LOG("get_image_size");
			maui_size size = { 0, 0 };
			_callbacks.get_image_size(src, baseurl, &size);

			sz.width = size.width;
			sz.height = size.height;
		}

		void maui_container::draw_image(litehtml::uint_ptr hdc, const litehtml::background_layer& layer, const std::string& url, const std::string& base_url) {
			LOG("draw_image");
			
			maui_background_paint mbg = maui_background_paint(layer);
			mbg.baseurl = base_url.c_str();
			mbg.image = url.c_str();
			mbg.position_x = layer.origin_box.x;
			mbg.position_y = layer.origin_box.y;
			mbg.image_size = { layer.origin_box.width, layer.origin_box.height };
			_callbacks.draw_background(mbg);			
		}

		void maui_container::draw_solid_fill(litehtml::uint_ptr hdc, const litehtml::background_layer& layer, const litehtml::web_color& color) {
			LOG("draw_solid_fill");
			maui_background_paint mbg = maui_background_paint(layer);
			mbg.color = color;
			_callbacks.draw_background(mbg);
		}
		void maui_container::draw_borders(litehtml::uint_ptr hdc, const litehtml::borders& borders, const litehtml::position& draw_pos, bool root)
		{
			LOG("draw_borders");
			_callbacks.draw_borders(borders, draw_pos, root);
		}
		void maui_container::set_caption(const char* caption)
		{
		}
		void maui_container::set_base_url(const char* base_url)
		{
		}
		void maui_container::link(const std::shared_ptr<litehtml::document>& doc, const litehtml::element::ptr& el)
		{
		}
		void maui_container::on_anchor_click(const char* url, const litehtml::element::ptr& el)
		{
			LOG("on_anchor_click");
			_callbacks.on_anchor_click(url);
		}
		void maui_container::on_mouse_event(const litehtml::element::ptr& el, litehtml::mouse_event event)
		{
		}
		void maui_container::set_cursor(const char* cursor)
		{
			LOG("set_cursor");
			_callbacks.set_cursor(cursor);
		}
		void maui_container::transform_text(litehtml::string& text, litehtml::text_transform tt)
		{
			if (text.empty()) return;
			switch (tt)
			{
			case litehtml::text_transform_capitalize:
				if (!text.empty())
				{
					text[0] = std::toupper(text.at(0));
				}
				break;
			case litehtml::text_transform_uppercase:
				for (std::basic_string<char>::iterator p = text.begin(); p != text.end(); ++p) {
					*p = std::toupper(*p); 
				}
				break;
			case litehtml::text_transform_lowercase:
				for (std::basic_string<char>::iterator p = text.begin(); p != text.end(); ++p) {
					*p = std::tolower(*p);
				}		
				break;
			case litehtml::text_transform_none:
				break;
			}
		}
		void maui_container::import_css(litehtml::string& text, const litehtml::string& url, litehtml::string& baseurl)
		{
            char* ptext;
            char* pbaseurl;
            _callbacks.import_css(&ptext, url.c_str(), &pbaseurl);
            if (ptext) {
                text = ptext;
				_callbacks.free_string(ptext);
            }
            if(pbaseurl) {
                baseurl = pbaseurl;
				_callbacks.free_string(pbaseurl);
            }
		}
		void maui_container::set_clip(const litehtml::position& pos, const litehtml::border_radiuses& bdr_radius)
		{
		}
		void maui_container::del_clip()
		{
		}
		void maui_container::get_client_rect(litehtml::position& client) const
		{
			LOG("get_client_rect");
			auto cbclient = _callbacks.get_client_rect();
			client.x = cbclient.x;
			client.y = cbclient.y;
			client.width = cbclient.width;
			client.height = cbclient.height;
			LOG("%u %u %u %u", client.x, client.y, client.width, client.height);
		}
		std::shared_ptr<litehtml::element> maui_container::create_element(const char* tag_name, const litehtml::string_map& attributes, const std::shared_ptr<litehtml::document>& doc)
		{
			// callback on element creation
			/*if (!t_strcmp(tag_name, _t("li")))
			{
				return std::make_shared<litehtml::el_ol>(doc);
			}*/

			return 0;
		}
		void maui_container::get_media_features(litehtml::media_features& media) const
		{
			LOG("get_media_features");
			litehtml::position client;
			get_client_rect(client);
			media.type = litehtml::media_type_screen;
			media.color = 8;
			media.width = client.width;
			media.height = client.height;			
			LOG("%u %u", media.width, media.height);
		}
		void maui_container::get_language(litehtml::string& language, litehtml::string& culture) const
		{
			LOG("get_language");
			defaults def;
			_callbacks.get_defaults(def);
			language = dupstr(def.language);
			culture = dupstr(def.culture);
		}
		litehtml::string maui_container::resolve_color(const litehtml::string& color) const
		{
			return litehtml::string();
		}

		
	}
}
