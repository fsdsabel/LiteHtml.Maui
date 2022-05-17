using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Text;
using Color = Windows.UI.Color;
using FontWeight = Windows.UI.Text.FontWeight;
using Rect = Windows.Foundation.Rect;

namespace LiteHtmlMaui.Handlers.Native
{
    class WindowsLiteHtmlDocumentView : LiteHtmlDocumentView<CanvasDrawingSession, ICanvasImage, CanvasFontFace>
    {
       class CanvasFontMetrics
        {
            public CanvasFontMetrics(CanvasFontFace fontFace)
            {
                Ascent = fontFace.Ascent;
                UnderlinePosition = fontFace.UnderlinePosition;
                UnderlineThickness = fontFace.UnderlineThickness;
                StrikethroughPosition = fontFace.StrikethroughPosition;
                StrikethroughThickness = fontFace.StrikethroughThickness;
                Descent = fontFace.Descent;
                XHeight = fontFace.LowercaseLetterHeight;
            }

            public float Ascent { get; private set; }
            public float UnderlinePosition { get; private set; }
            public float UnderlineThickness { get; private set; }
            public float StrikethroughPosition { get; private set; }
            public float StrikethroughThickness { get; private set; }
            public float Descent { get; private set; }
            public float XHeight { get; private set; }
        }


        private CanvasDrawingSession? _drawingSession;        
        private readonly ICanvasResourceCreator _resourceCreator;
        private readonly Func<float> _dpiResolver;
        private readonly Action<string> _setCursorAction;
        private static readonly Dictionary<(string faceName, int weight, FontStyle fontStyle), CanvasFontMetrics> _fontMetricsCache = new Dictionary<(string faceName, int weight, FontStyle fontStyle), CanvasFontMetrics>();

        public WindowsLiteHtmlDocumentView(
            ICanvasResourceCreator resourceCreator, 
            Func<float> dpiResolver,  
            LiteHtmlResolveResourceDelegate resolveResource,
            LiteHtmlRedrawView redrawView,
            Action<string> setCursorAction)
            : base(resolveResource, redrawView)
        {
            _dpiResolver = dpiResolver;
            _setCursorAction = setCursorAction;
            _resourceCreator = resourceCreator;            
        }

        protected override async Task<ICanvasImage> CreatePlatformBitmapAsync(Stream stream)
        {
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;
            var imgStream = ms.AsRandomAccessStream();
            var cimg = await CanvasVirtualBitmap.LoadAsync(_resourceCreator, imgStream);                        
            return cimg;
        }

        protected override void DrawBackgroundCb(ref BackgroundPaint bg)
        {
            if (_drawingSession == null) return;



            if (!string.IsNullOrEmpty(bg.Image))
            {
                // draw image
                var img = GetImage(CombineUrl(bg.BaseUrl, bg.Image));
                if (img != null)
                {
                    _drawingSession.DrawImage(
                        img.Image,
                        new Rect(bg.PositionX, bg.PositionY, bg.ImageSize.Width, bg.ImageSize.Height),
                        img.Image.GetBounds(_drawingSession.Device));
                }
            }
            else
            {
                // we do not support multiple colors/thicknesses on borders or styles .. keep it simple, but this can be expanded if necessary                
                var color = bg.Color;
                using var brush = new CanvasSolidColorBrush(_drawingSession.Device, Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue));


                var b = new Borders();
                b.Radius = bg.BorderRadius;
                var position = bg.OriginBox;
                using var path = CreateRoundedRect(_drawingSession.Device, ref b, ref position, true);

                _drawingSession.FillGeometry(path, brush);
            }
        }

        protected override void DrawBordersCb(ref Borders borders, ref Position position, bool root)
        {
            if (borders.Left.Width == 0 &&
                borders.Right.Width == 0 &&
                borders.Top.Width == 0 &&
                borders.Bottom.Width == 0)
            {
                return;
            }

            if (_drawingSession == null) return;

            // we do not support multiple colors/thicknesses on borders            
            var borderColor = borders.Right.Color;
            var color = Color.FromArgb(borderColor.Alpha, borderColor.Red, borderColor.Green, borderColor.Blue);            
            var style = new CanvasStrokeStyle();
            switch (borders.Right.Style)
            {
                case BorderStyle.border_style_none:
                    return;
                case BorderStyle.border_style_solid:
                    style.DashStyle = CanvasDashStyle.Solid;
                    break;
                case BorderStyle.border_style_dashed:
                    style.DashStyle = CanvasDashStyle.Dash;
                    break;
                case BorderStyle.border_style_dotted:
                    style.DashStyle = CanvasDashStyle.Dot;
                    break;
                default:
                    style.DashStyle = CanvasDashStyle.Solid;
                    break;
            }

            using var path = CreateRoundedRect(_drawingSession.Device, ref borders, ref position, false);
            _drawingSession.DrawGeometry(path, color, borders.Right.Width, style);
        }

        protected override void DrawListMarkerCb(ref ListMarker listMarker, ref FontDesc font)
        {
            if (_drawingSession == null) return;

            if (string.IsNullOrEmpty(listMarker.Image))
            {
                var color = Color.FromArgb(listMarker.color.Alpha, listMarker.color.Red, listMarker.color.Green, listMarker.color.Blue);
                switch(listMarker.marker_type)
                {
                    case list_style_type.list_style_type_circle:
                        _drawingSession.DrawEllipse(listMarker.pos.X + listMarker.pos.Width / 2, listMarker.pos.Y + listMarker.pos.Height / 2,
                            listMarker.pos.Width / 2, listMarker.pos.Height / 2, color);
                        break;
                    case list_style_type.list_style_type_disc:
                        _drawingSession.FillEllipse(listMarker.pos.X + listMarker.pos.Width / 2, listMarker.pos.Y + listMarker.pos.Height / 2,
                            listMarker.pos.Width / 2, listMarker.pos.Height / 2, color);
                        break;
                    case list_style_type.list_style_type_square:
                        _drawingSession.FillRectangle(new Rect(listMarker.pos.X, listMarker.pos.Y, listMarker.pos.Width, listMarker.pos.Height), color);
                        break;
                    case list_style_type.list_style_type_decimal:
                    case list_style_type.list_style_type_lower_alpha:
                    case list_style_type.list_style_type_lower_latin:
                    case list_style_type.list_style_type_upper_alpha:
                    case list_style_type.list_style_type_upper_latin:
                        string text = "";
                        switch (listMarker.marker_type)
                        {
                            case list_style_type.list_style_type_decimal:
                                text = "." + listMarker.index;
                                break;
                            case list_style_type.list_style_type_lower_latin:
                            case list_style_type.list_style_type_lower_alpha:
                                text = "." + IndexToAlpha(listMarker.index, "abcdefghijklmnopqrstuvwxyz");
                                break;
                            case list_style_type.list_style_type_upper_latin:
                            case list_style_type.list_style_type_upper_alpha:
                                text = "." + IndexToAlpha(listMarker.index, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
                                break;
                        }
                        var textFormat = CreateTextFormatFromFontDesc(ref font);
                        textFormat.Direction = CanvasTextDirection.RightToLeftThenTopToBottom;
                        _drawingSession.DrawText(text, listMarker.pos.X, listMarker.pos.Y, color, textFormat);
                        break;
                }
            }           
        }

        protected override void DrawTextCb(IntPtr hdc, string text, ref FontDesc font, ref WebColor color, ref Position position)
        {
            if (_drawingSession == null) return;

            using var format = CreateTextFormatFromFontDesc(ref font);
            using var brush = new CanvasSolidColorBrush(_drawingSession.Device, Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue));
            using var textLayout = new CanvasTextLayout(_drawingSession.Device, text, format, position.Width, position.Height);
            
            CanvasFontMetrics? winfont = null;
            if (font.Decoration.HasFlag(font_decoration.Underline))
            {
                winfont = winfont ?? GetCanvasFontMetrics(ref font);
                var underlinePosition = position.Y + (winfont.Ascent + winfont.Descent) * format.FontSize;
                _drawingSession.DrawLine(position.X, underlinePosition, position.X + position.Width, underlinePosition, brush, format.FontSize * winfont.UnderlineThickness);               
                
            }
            if (font.Decoration.HasFlag(font_decoration.Linethrough))
            {
                winfont = winfont ?? GetCanvasFontMetrics(ref font);
                var strikethroughPosition = position.Y + (winfont.Ascent + winfont.Descent) * format.FontSize * 0.6f - 0.5f;
                _drawingSession.DrawLine(position.X, strikethroughPosition, position.X + position.Width, strikethroughPosition, brush, format.FontSize * winfont.StrikethroughThickness);
            }
            if (font.Decoration.HasFlag(font_decoration.Overline))
            {
                winfont = winfont ?? GetCanvasFontMetrics(ref font);
                var overlinePosition = position.Y - 0.5f;
                _drawingSession.DrawLine(position.X, overlinePosition, position.X + position.Width, overlinePosition, brush, format.FontSize * winfont.UnderlineThickness);
            }
            
            _drawingSession.DrawTextLayout(textLayout, position.X, position.Y, brush);
            
        }

        private CanvasFontMetrics GetCanvasFontMetrics(ref FontDesc font)
        {
            if(_fontMetricsCache.TryGetValue((font.FaceName, font.Weight, font.Italic), out var metrics))
            {
                return metrics;
            }

            var winfont = ResolveFont(ref font, (fontDesc, force) =>
            {
                using var fonts = CanvasFontSet.GetSystemFontSet().GetMatchingFonts(
                     fontDesc.FaceName,
                     new FontWeight((ushort)fontDesc.Weight),
                     FontStretch.Normal,
                     fontDesc.Italic == FontStyle.fontStyleItalic ? Windows.UI.Text.FontStyle.Italic : Windows.UI.Text.FontStyle.Normal);
                var winfont = fonts.Fonts.FirstOrDefault();
                if (winfont == null && force)
                {
                    winfont = CanvasFontSet.GetSystemFontSet().Fonts.First();
                }
                return winfont;
            });

            
           
            
            metrics = new CanvasFontMetrics(winfont);
            _fontMetricsCache.Add((font.FaceName, font.Weight, font.Italic), metrics);

            winfont.Dispose();
            return metrics;
        }



        protected override void FillFontMetricsCb(ref FontDesc font, ref FontMetrics fm)
        {
            Debug.WriteLine(font.FaceName);
            var winfont = GetCanvasFontMetrics(ref font);
            var scaledSize = PxToPt(font.Size);

            fm.Ascent = (int)Math.Ceiling(winfont.Ascent * scaledSize);
            fm.Descent = (int)Math.Ceiling(winfont.Descent* scaledSize);
            fm.Height = fm.Ascent + fm.Descent;            
            fm.XHeight = (int)Math.Ceiling(winfont.XHeight * scaledSize);
            fm.DrawSpaces = (font.Italic == FontStyle.fontStyleItalic || font.Decoration != 0) ? 1 : 0;            
        }

        protected override void GetDefaultsCb(ref Defaults defaults)
        {
            //defaults = new Defaults();
            defaults.Culture = Thread.CurrentThread.CurrentUICulture.ToString();
            defaults.Language = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
            var tb = new TextBlock();            
            defaults.FontFaceName = tb.FontFamily.Source; // default
            defaults.FontSize = PtToPxCb((int)tb.FontSize);
        }

        protected override MauiSize GetImageSize(ICanvasImage image)
        {
            var size = image.GetBounds(_resourceCreator);
            return new MauiSize((int)size.Width, (int)size.Height);
        }

        protected override int PtToPxCb(int pt)
        {
            return (int)(_dpiResolver() / 96.0f * pt);
        }

        private int PxToPt(int px)
        {
            return (int)Math.Ceiling(96.0f * px / _dpiResolver());
        }

        protected override void SetCursorCb(string cursor)
        {
            _setCursorAction(cursor);
        }

        protected override int TextWidthCb(string text, ref FontDesc font)
        {          
            using var format = CreateTextFormatFromFontDesc(ref font);

            using var textLayout = new CanvasTextLayout(_resourceCreator, text, format, float.MaxValue, float.MaxValue);
            return (int)Math.Floor(textLayout.LayoutBoundsIncludingTrailingWhitespace.Width);            
        }

       
        public override void DrawDocument(CanvasDrawingSession hdc, int width, int height)
        {
            _drawingSession = hdc;
            base.DrawDocument(hdc, width, height);
            _drawingSession = null;
        }

        private CanvasTextFormat CreateTextFormatFromFontDesc(ref FontDesc fontDesc)
        {
            var format = new CanvasTextFormat();

            format.FontFamily = fontDesc.FaceName;
            format.FontSize = PxToPt(fontDesc.Size);
            format.FontWeight = new FontWeight((ushort)fontDesc.Weight);
            format.FontStyle = fontDesc.Italic == FontStyle.fontStyleItalic ? Windows.UI.Text.FontStyle.Italic : Windows.UI.Text.FontStyle.Normal;
            format.Options = CanvasDrawTextOptions.Default;
            format.WordWrapping = CanvasWordWrapping.NoWrap;

            return format;
        }

        private static CanvasGeometry CreateRoundedRect(ICanvasResourceCreator canvasResourceCreator, ref Borders borders, ref Position draw_pos, bool forFill)
        {
            
            var radii = new Radii(ref borders, !forFill);

            var rect = new Rect(draw_pos.X, draw_pos.Y, draw_pos.Width, draw_pos.Height);
            var point = new Vector2(radii.LeftTop, 0.0f);
            var point2 = new Vector2((float)(rect.Width - radii.RightTop), 0.0f);
            var point3 = new Vector2((float)rect.Width, radii.TopRight);
            var point4 = new Vector2((float)rect.Width, (float)(rect.Height - radii.BottomRight));
            var point5 = new Vector2((float)(rect.Width - radii.RightBottom), (float)rect.Height);
            var point6 = new Vector2(radii.LeftBottom, (float)rect.Height);
            var point7 = new Vector2(0.0f, (float)(rect.Height - radii.BottomLeft));
            var point8 = new Vector2(0.0f, radii.TopLeft);
            
            if (point.X > point2.X)
            {
                float x = (float)(radii.LeftTop / (radii.LeftTop + radii.RightTop) * rect.Width);
                point.X = x;
                point2.X = x;
            }
            if (point3.Y > point4.Y)
            {
                float y = (float)(radii.TopRight / (radii.TopRight + radii.BottomRight) * rect.Height);
                point3.Y = y;
                point4.Y = y;
            }
            if (point5.X < point6.X)
            {
                float x2 = (float)(radii.LeftBottom / (radii.LeftBottom + radii.RightBottom) * rect.Width);
                point5.X = x2;
                point6.X = x2;
            }
            if (point7.Y < point8.Y)
            {
                float y2 = (float)(radii.TopLeft / (radii.TopLeft + radii.BottomLeft) * rect.Height);
                point7.Y = y2;
                point8.Y = y2;
            }
            var vector = new Vector2(new float[] { (float)rect.X, (float)rect.Y });
            
            point += vector;
            point2 += vector;
            point3 += vector;
            point4 += vector;
            point5 += vector;
            point6 += vector;
            point7 += vector;
            point8 += vector;

            // align to pixel raster

            if (borders.Left.Width >= 1)
            {
                var adjfactor = (float)borders.Left.Width / 2;
                var adjustx = new Vector2(adjfactor, 0);
                var adjusty = new Vector2(0, adjfactor);
                var adjust = new Vector2(adjustx.X, adjusty.Y);
                point = clampPositive(point + adjust);
                point2 = clampPositive(point2 - adjustx + adjusty);
                point3 = clampPositive(point3 - adjustx + adjusty);
                point4 = clampPositive(point4 - adjust);
                point5 = clampPositive(point5 - adjust);
                point6 = clampPositive(point6 + adjustx - adjusty);
                point7 = clampPositive(point7 + adjustx - adjusty);
                point8 = clampPositive(point8 + adjust);
            }

            // platform specific
            var path = new CanvasPathBuilder(canvasResourceCreator);
            
            

            path.BeginFigure(point.X, point.Y);
            path.AddLine(point2.X, point2.Y);

            float num = (float)(rect.Right - point2.X);
            float num2 = (float)(point3.Y - rect.Y);
            float halfstroke = (float)(borders.Left.Width) / 2;

            if (Math.Abs(num) > halfstroke || Math.Abs(num2) > halfstroke)
            {
                path.AddArc(point3, num, num2, (float)Math.PI/2, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);             
            }
            path.AddLine(point4.X, point4.Y); 
            num = (float)(rect.Right - point5.X);
            num2 = (float)(rect.Bottom - point4.Y);
            if (Math.Abs(num) > halfstroke || Math.Abs(num2) > halfstroke)
            {
                path.AddArc(point5, num, num2, (float)Math.PI / 2, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
            }
            path.AddLine(point6.X, point6.Y);
            num = (float)(point6.X - rect.X);
            num2 = (float)(rect.Bottom - point7.Y);
            if (Math.Abs(num) > halfstroke || Math.Abs(num2) > halfstroke)
            {
                path.AddArc(point7, num, num2, (float)Math.PI / 2, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
            }
            path.AddLine(point8.X, point8.Y);
            num = (float)(point.X - rect.X);
            num2 = (float)(point8.Y - rect.Y);
            if (Math.Abs(num) > halfstroke || Math.Abs(num2) > halfstroke)
            {
                path.AddArc(point, num, num2, (float)Math.PI / 2, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
            }
            path.EndFigure(CanvasFigureLoop.Closed);
            return CanvasGeometry.CreatePath(path);

            Vector2 clampPositive(Vector2 v)
            {
                if (v.X < 0) v.X = 0;
                if (v.Y < 0) v.Y = 0;
                return v;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
               // _resourceCreator.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    struct Radii
    {
        public float LeftTop;
        public float TopLeft;
        public float TopRight;
        public float RightTop;
        public float RightBottom;
        public float BottomRight;
        public float BottomLeft;
        public float LeftBottom;

        public Radii(ref Borders borders, bool outer) 
        {
            float num = 0.5f * borders.Left.Width;
            float num2 = 0.5f * borders.Top.Width;
            float num3 = 0.5f * borders.Right.Width;
            float num4 = 0.5f * borders.Bottom.Width;
            if (!outer)
            {
                LeftTop = Math.Max(0.0f, borders.Radius.top_left_x - num);
                TopLeft = Math.Max(0.0f, borders.Radius.top_left_x - num2);
                TopRight = Math.Max(0.0f, borders.Radius.top_right_x - num2);
                RightTop = Math.Max(0.0f, borders.Radius.top_right_x - num3);
                RightBottom = Math.Max(0.0f, borders.Radius.bottom_right_x - num3);
                BottomRight = Math.Max(0.0f, borders.Radius.bottom_right_x - num4);
                BottomLeft = Math.Max(0.0f, borders.Radius.bottom_left_x - num4);
                LeftBottom = Math.Max(0.0f, borders.Radius.bottom_left_x - num);
                return;
            }
            if (borders.Radius.top_left_x == 0)
            {
                LeftTop = TopLeft = 0.0f;
            }
            else
            {
                LeftTop = borders.Radius.top_left_x + num;
                TopLeft = borders.Radius.top_left_x + num2;
            }
            if (borders.Radius.top_right_x == 0)
            {
                TopRight = RightTop = 0.0f;
            }
            else
            {
                TopRight = borders.Radius.top_right_x + num2;
                RightTop = borders.Radius.top_right_x + num3;
            }
            if (borders.Radius.bottom_right_x == 0)
            {
                RightBottom = BottomRight = 0.0f;
            }
            else
            {
                RightBottom = borders.Radius.bottom_right_x + num3;
                BottomRight = borders.Radius.bottom_right_x + num4;
            }
            if (borders.Radius.bottom_left_x == 0)
            {
                BottomLeft = LeftBottom = 0.0f;
                return;
            }
            BottomLeft = borders.Radius.bottom_left_x + num4;
            LeftBottom = borders.Radius.bottom_left_x + num;
        }
    };

}
