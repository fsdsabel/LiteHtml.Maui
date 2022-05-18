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
		tchar_t* dupstr(tchar_t* str) {
			if (str == nullptr) return nullptr;
#ifdef LITEHTML_UTF8
			return (tchar_t*)strdup(str);
#else
			return (tchar_t*)_wcsdup(str);
#endif
		}

		litehtml::uint_ptr maui_container::create_font(const litehtml::tchar_t* faceName, int size, int weight, litehtml::font_style italic, unsigned int decoration, litehtml::font_metrics* fm)
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
		int maui_container::text_width(const litehtml::tchar_t* text, litehtml::uint_ptr hFont)
		{
			LOG("text_width");
			return _callbacks.text_width(text, _fonts.at(hFont));
		}
		void maui_container::draw_text(litehtml::uint_ptr hdc, const litehtml::tchar_t* text, litehtml::uint_ptr hFont, litehtml::web_color color, const litehtml::position& pos)
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
		const litehtml::tchar_t* maui_container::get_default_font_name() const
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
		void maui_container::load_image(const litehtml::tchar_t* src, const litehtml::tchar_t* baseurl, bool redraw_on_ready)
		{
			LOG("load_image");
			_callbacks.load_image(src, baseurl, redraw_on_ready);
		}
		void maui_container::get_image_size(const litehtml::tchar_t* src, const litehtml::tchar_t* baseurl, litehtml::size& sz)
		{
			LOG("get_image_size");
			maui_size size = { 0, 0 };
			_callbacks.get_image_size(src, baseurl, &size);

			sz.width = size.width;
			sz.height = size.height;
		}

		void maui_container::draw_background(litehtml::uint_ptr hdc, const litehtml::background_paint& bg)
		{
			LOG("draw_background");
			maui_background_paint mbg = maui_background_paint(bg);
			_callbacks.draw_background(mbg);
		}
		void maui_container::draw_borders(litehtml::uint_ptr hdc, const litehtml::borders& borders, const litehtml::position& draw_pos, bool root)
		{
			LOG("draw_borders");
			_callbacks.draw_borders(borders, draw_pos, root);
		}
		void maui_container::set_caption(const litehtml::tchar_t* caption)
		{
		}
		void maui_container::set_base_url(const litehtml::tchar_t* base_url)
		{
		}
		void maui_container::link(const std::shared_ptr<litehtml::document>& doc, const litehtml::element::ptr& el)
		{
		}
		void maui_container::on_anchor_click(const litehtml::tchar_t* url, const litehtml::element::ptr& el)
		{
			LOG("on_anchor_click");
			_callbacks.on_anchor_click(url);
		}
		void maui_container::set_cursor(const litehtml::tchar_t* cursor)
		{
			LOG("set_cursor");
			_callbacks.set_cursor(cursor);
		}
		void maui_container::transform_text(litehtml::tstring& text, litehtml::text_transform tt)
		{
			if (text.empty()) return;
			switch (tt)
			{
			case litehtml::text_transform_capitalize:
				#ifdef LITEHTML_UTF8
				#else
				if (!text.empty())
				{
					text[0] = std::towupper(text.at(0));
				}
				#endif		
				break;
			case litehtml::text_transform_uppercase:
				#ifdef LITEHTML_UTF8
				#else
				for (std::basic_string<wchar_t>::iterator p = text.begin(); p != text.end(); ++p) {
					*p = std::towupper(*p); 
				}
				#endif
				break;
			case litehtml::text_transform_lowercase:
				#ifdef LITEHTML_UTF8

				#else
				for (std::basic_string<wchar_t>::iterator p = text.begin(); p != text.end(); ++p) {
					*p = std::towlower(*p);
				}
				#endif			
				break;
			case litehtml::text_transform_none:
				break;
			}
		}
		void maui_container::import_css(litehtml::tstring& text, const litehtml::tstring& url, litehtml::tstring& baseurl)
		{
            litehtml::tchar_t* ptext;
            litehtml::tchar_t* pbaseurl;
            _callbacks.import_css(&ptext, url.c_str(), &pbaseurl);
            if (ptext) {
                text = ptext;
				free(ptext);
            }
            if(pbaseurl) {
                baseurl = pbaseurl;
				free(pbaseurl);
            }
		}
		void maui_container::set_clip(const litehtml::position& pos, const litehtml::border_radiuses& bdr_radius, bool valid_x, bool valid_y)
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
		std::shared_ptr<litehtml::element> maui_container::create_element(const litehtml::tchar_t* tag_name, const litehtml::string_map& attributes, const std::shared_ptr<litehtml::document>& doc)
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
		void maui_container::get_language(litehtml::tstring& language, litehtml::tstring& culture) const
		{
			LOG("get_language");
			defaults def;
			_callbacks.get_defaults(def);
			language = dupstr(def.language);
			culture = dupstr(def.culture);
		}
		litehtml::tstring maui_container::resolve_color(const litehtml::tstring& color) const
		{
			return litehtml::tstring();
		}

		
	}
}
