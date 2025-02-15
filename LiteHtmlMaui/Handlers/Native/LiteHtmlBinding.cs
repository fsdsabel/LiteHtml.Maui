﻿#if WINDOWS
#define UTF8_SUPPORTED
#endif
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LiteHtmlMaui.Handlers.Native
{
    enum FontStyle
    {
        fontStyleNormal,
        fontStyleItalic
    };

    [StructLayout(LayoutKind.Sequential)]
    struct FontMetrics
    {
        public int FontSize;
        public int Height;
        public int Ascent;
        public int Descent;
        public int XHeight;
        public int CharWidth;
        public int DrawSpaces;

    }

    [StructLayout(LayoutKind.Sequential, CharSet = LiteHtmlInterop.InteropCharSet)]
    record struct FontDesc
    {
        #if UTF8_SUPPORTED
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        #endif
        public string FaceName;
        public int Size;
        public int Weight;
        public FontStyle Italic;
        public font_decoration Decoration;       
        public IntPtr FontMetrics;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Position
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WebColor
    {
        public byte Red;
        public byte Green;
        public byte Blue;        
        public byte Alpha;
        public bool IsCurrentColor;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = LiteHtmlInterop.InteropCharSet)]
    struct Defaults
    {
        public int FontSize;
#if UTF8_SUPPORTED
        [MarshalAs(UnmanagedType.LPUTF8Str)]
#endif
        public string FontFaceName;
#if UTF8_SUPPORTED
        [MarshalAs(UnmanagedType.LPUTF8Str)]
#endif
        public string Language;
#if UTF8_SUPPORTED
        [MarshalAs(UnmanagedType.LPUTF8Str)]
#endif
        public string Culture;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MauiSize
    {
        public int Width;
        public int Height;
                

        public MauiSize(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

    enum BorderStyle
    {
        border_style_none,
        border_style_hidden,
        border_style_dotted,
        border_style_dashed,
        border_style_solid,
        border_style_double,
        border_style_groove,
        border_style_ridge,
        border_style_inset,
        border_style_outset
    };

    [StructLayout(LayoutKind.Sequential)]
    struct Border
    {
        public int Width;
        public BorderStyle Style;
        public WebColor Color;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BorderRadiuses
    {
        public int top_left_x;
        public int top_left_y;
        public int top_right_x;
        public int top_right_y;
        public int bottom_right_x;
        public int bottom_right_y;
        public int bottom_left_x;
        public int bottom_left_y;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Borders
    {
        public Border Left;
        public Border Top;
        public Border Right;
        public Border Bottom;
        public BorderRadiuses Radius;
    }

    enum background_attachment
    {
        background_attachment_scroll,
        background_attachment_fixed
    };

    enum background_repeat
    {
        background_repeat_repeat,
        background_repeat_repeat_x,
        background_repeat_repeat_y,
        background_repeat_no_repeat
    };

    [Flags]
    enum font_decoration
    {
        None = 0,
        Underline = 1,
        Linethrough = 2,
        Overline = 4
    }

    [StructLayout(LayoutKind.Sequential, CharSet = LiteHtmlInterop.InteropCharSet)]
    struct BackgroundPaint
    {
#if UTF8_SUPPORTED
        [MarshalAs(UnmanagedType.LPUTF8Str)]
#endif
        public string Image;
#if UTF8_SUPPORTED
        [MarshalAs(UnmanagedType.LPUTF8Str)]
#endif
        public string BaseUrl;
        public background_attachment attachment;
        public background_repeat repeat;
        public WebColor Color;
        public Position ClipBox;
        public Position OriginBox;
        public Position BorderBox;
        public BorderRadiuses BorderRadius;
        public MauiSize ImageSize;
        public int PositionX;
        public int PositionY;
        public bool IsRoot;
    }

    enum LiteHtmlEvent
    {
        None,
        Move,
        Down,
        Up,
        Leave
    }

    enum list_style_type
    {
        list_style_type_none,
        list_style_type_circle,
        list_style_type_disc,
        list_style_type_square,
        list_style_type_armenian,
        list_style_type_cjk_ideographic,
        list_style_type_decimal,
        list_style_type_decimal_leading_zero,
        list_style_type_georgian,
        list_style_type_hebrew,
        list_style_type_hiragana,
        list_style_type_hiragana_iroha,
        list_style_type_katakana,
        list_style_type_katakana_iroha,
        list_style_type_lower_alpha,
        list_style_type_lower_greek,
        list_style_type_lower_latin,
        list_style_type_lower_roman,
        list_style_type_upper_alpha,
        list_style_type_upper_latin,
        list_style_type_upper_roman,
    };


    [StructLayout(LayoutKind.Sequential, CharSet = LiteHtmlInterop.InteropCharSet)]
    struct ListMarker
    {
#if UTF8_SUPPORTED
        [MarshalAs(UnmanagedType.LPUTF8Str)]
#endif
        public string Image;
#if UTF8_SUPPORTED
        [MarshalAs(UnmanagedType.LPUTF8Str)]
#endif
        public string BaseUrl;
        public list_style_type marker_type;
        public WebColor color;
        public Position pos;
        public int index;
        public UIntPtr font;
    };


    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = LiteHtmlInterop.InteropCharSet)]
#if UTF8_SUPPORTED
    delegate int TextWidthDelegate([In][MarshalAs(UnmanagedType.LPUTF8Str)] string text, [In] ref FontDesc font);
#else
    delegate int TextWidthDelegate([In] string text, [In] ref FontDesc font);
#endif

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate Position GetClientRectDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = LiteHtmlInterop.InteropCharSet)]
#if UTF8_SUPPORTED
    delegate void DrawTextDelegate([In] IntPtr hdc, [In] [MarshalAs(UnmanagedType.LPUTF8Str)] string text, [In] ref FontDesc font, [In] ref WebColor color, [In] ref Position position);
#else
    delegate void DrawTextDelegate([In] IntPtr hdc, [In] string text, [In] ref FontDesc font, [In] ref WebColor color, [In] ref Position position);
#endif

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void FillFontMetricsDelegate([In] ref FontDesc font, [In, Out] ref FontMetrics fm);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void DrawBackgroundDelegate([In] ref BackgroundPaint bg);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = LiteHtmlInterop.InteropCharSet)]
#if UTF8_SUPPORTED
    delegate void SetCursorDelegate([In][MarshalAs(UnmanagedType.LPUTF8Str)] string cursor);
#else
    delegate void SetCursorDelegate([In] string cursor);
#endif

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void DrawBordersDelegate([In] ref Borders borders, [In] ref Position position, [In] bool root);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = LiteHtmlInterop.InteropCharSet)]
#if UTF8_SUPPORTED
    delegate void LoadImageDelegate([In] string source, [In] [MarshalAs(UnmanagedType.LPUTF8Str)] string baseUrl, [In] bool redrawOnReady);
#else
    delegate void LoadImageDelegate([In] string source, [In] string baseUrl, [In] bool redrawOnReady);
#endif

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = LiteHtmlInterop.InteropCharSet)]
#if UTF8_SUPPORTED
    delegate void GetImageSizeDelegate([In][MarshalAs(UnmanagedType.LPUTF8Str)] string source, [In][MarshalAs(UnmanagedType.LPUTF8Str)] string baseUrl, [In, Out] ref MauiSize size);
#else
    delegate void GetImageSizeDelegate([In] string source, [In] string baseUrl, [In, Out] ref MauiSize size);
#endif

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void DrawListMarkerDelegate([In] ref ListMarker listMarker, [In] ref FontDesc font);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void GetDefaultsDelegate(ref Defaults defaults);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = LiteHtmlInterop.InteropCharSet)]
#if UTF8_SUPPORTED
    delegate void OnAnchorClickDelegate([In][MarshalAs(UnmanagedType.LPUTF8Str)] string url);
#else
    delegate void OnAnchorClickDelegate([In] string url);
#endif

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int PtToPxDelegate([In] int pt);

    // StringBuilder instead of IntPtr would be much more elegant but doesn't work for iOS
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = LiteHtmlInterop.InteropCharSet)]
#if UTF8_SUPPORTED
    delegate void ImportCssDelegate([Out] out IntPtr text, [In][MarshalAs(UnmanagedType.LPUTF8Str)] string url, [Out] out IntPtr baseUrl);
#else
    delegate void ImportCssDelegate([Out] out IntPtr text, [In] string url, [Out] out IntPtr baseUrl);
#endif

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void FreeStringDelegate([In] IntPtr str);

    [StructLayout(LayoutKind.Sequential)]
    struct MauiContainerCallbacks
    {
        public TextWidthDelegate TextWidth;
        public GetClientRectDelegate GetClientRect;
        public DrawTextDelegate DrawText;
        public FillFontMetricsDelegate FillFontMetrics;
        public DrawBackgroundDelegate DrawBackground;
        public SetCursorDelegate SetCursor;
        public LoadImageDelegate LoadImage;
        public GetImageSizeDelegate GetImageSize;
        public DrawBordersDelegate DrawBorders;
        public DrawListMarkerDelegate DrawListMarker;
        public GetDefaultsDelegate GetDefaults;
        public OnAnchorClickDelegate OnAnchorClick;
        public PtToPxDelegate PtToPx;
        public ImportCssDelegate ImportCss;
        public FreeStringDelegate FreeString;
    }

    class LiteHtmlDocumentSafeHandle : SafeHandle
    {
        public LiteHtmlDocumentSafeHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            LiteHtmlInterop.DestroyDocument(handle);
            handle = IntPtr.Zero;
            return true;
        }
    }

    static class LiteHtmlInterop
    {
#if __ANDROID__
        const string InteropDll = "liblitehtml-maui.so";
        public const CharSet InteropCharSet = CharSet.Ansi;
#elif WINDOWS
        const string InteropDll = "litehtml-maui.dll";
        public const CharSet InteropCharSet = CharSet.Ansi;
#elif IOS
        const string InteropDll = "__Internal";
        public const CharSet InteropCharSet = CharSet.Ansi;
#else
        const string InteropDll = "liblitehtml-maui.so";
        public const CharSet InteropCharSet = CharSet.Ansi;
#endif

        [DllImport(InteropDll, EntryPoint = "create_document", CharSet = InteropCharSet)]
#if UTF8_SUPPORTED
        public static extern LiteHtmlDocumentSafeHandle CreateDocument(MauiContainerCallbacks callbacks, [MarshalAs(UnmanagedType.LPUTF8Str)] string html, [MarshalAs(UnmanagedType.LPUTF8Str)] string? masterCss, [MarshalAs(UnmanagedType.LPUTF8Str)] string? userCss);
#else
        public static extern LiteHtmlDocumentSafeHandle CreateDocument(MauiContainerCallbacks callbacks, string html, string? masterCss, string? userCss);
#endif

        [DllImport(InteropDll, EntryPoint = "destroy_document")]
        public static extern void DestroyDocument(IntPtr document);

        [DllImport(InteropDll, EntryPoint = "measure_document")]
        public static extern MauiSize MeasureDocument(LiteHtmlDocumentSafeHandle document, MauiSize availableSize);

        [DllImport(InteropDll, EntryPoint = "draw_document")]
        public static extern void DrawDocument(LiteHtmlDocumentSafeHandle document, IntPtr hdc, MauiSize size);

        [DllImport(InteropDll, EntryPoint = "report_event")]
        public static extern bool ReportEvent(LiteHtmlDocumentSafeHandle document, LiteHtmlEvent e, int x, int y, int client_x, int client_y);
    }
}
