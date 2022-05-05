using CoreGraphics;
using Foundation;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UIKit;

namespace LiteHtmlMaui.Handlers.Native
{
    class IOSLiteHtmlDocumentView : LiteHtmlDocumentView<CGContext, UIImage>
    {
       

        private CGContext? _hdc;

        public IOSLiteHtmlDocumentView(LiteHtmlResolveResourceDelegate resolveResource)
         : base(resolveResource)
        {
        }

        protected override Task<UIImage> CreatePlatformBitmapAsync(Stream stream)
        {
            using var imageData = NSData.FromStream(stream);
            return Task.FromResult(UIImage.LoadFromData(imageData) ?? throw new InvalidOperationException("Cannot create platform bitmap from stream."));
        }

        protected override void DrawBackgroundCb(ref BackgroundPaint bg)
        {
            if (!VerifyContext()) return;

            if (!string.IsNullOrEmpty(bg.Image))
            {
                // draw image
                var img = GetImage(CombineUrl(bg.BaseUrl, bg.Image));
                if (img != null)
                {
                    img.Image.Draw(new CGRect(bg.PositionX, bg.PositionY, bg.ImageSize.Width, bg.ImageSize.Height));
                    img.Release();
                }
            }
            else
            {
                // we do not support multiple colors/thicknesses on borders or styles .. keep it simple, but this can be expanded if necessary

                var color = Color(bg.Color);

                var b = new Borders();
                b.Radius = bg.BorderRadius;
                var position = bg.OriginBox;
                
                var path = CreateRoundedRect(ref b, ref position, true);
                _hdc.AddPath(path);
                
                _hdc.SetFillColor(color);                
                _hdc.FillPath();
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

            if (!VerifyContext()) return;

            // we do not support multiple colors/thicknesses on borders            
            var borderColor = borders.Right.Color;
            var color = Color(borderColor);
            NFloat[] style;
            switch (borders.Right.Style)
            {
                case BorderStyle.border_style_none:
                    return;
                case BorderStyle.border_style_solid:
                    style = new NFloat[0];
                    break;
                case BorderStyle.border_style_dashed:
                    style = new NFloat[] { 3, 2 };
                    break;
                case BorderStyle.border_style_dotted:
                    style = new NFloat[] { 1, 1 };
                    break;
                default:
                    style = new NFloat[0];
                    break;
            }

            using var path = CreateRoundedRect(ref borders, ref position, false);
            _hdc.SetStrokeColor(color);
            _hdc.SetLineWidth(borders.Right.Width);
            _hdc.SetLineDash(0, style);
            _hdc.StrokePath();
        }

        protected override void DrawListMarkerCb(IntPtr listMarker, ref FontDesc font)
        {
           // throw new NotImplementedException();
        }

        protected override void DrawTextCb(IntPtr hdc, string text, ref FontDesc font, ref WebColor color, ref Position position)
        {
            if (!VerifyContext()) return;

            var attributes = FontAttributesFromFontDesc(ref font);
            attributes.ForegroundColor = new UIColor(Color(color));
            new NSString(text).DrawString(
                new CGRect(position.X, position.Y, position.Width, position.Height), attributes);            
        }

        protected override void FillFontMetricsCb(ref FontDesc font, ref FontMetrics fm)
        {

            var uifont = UIFont.FromName(font.FaceName, font.Size);
            
            fm.Ascent = (int)uifont.Ascender;
            fm.Descent = -(int)uifont.Descender;
            fm.Height = (int)Math.Ceiling(uifont.LineHeight);
            fm.XHeight = (int)Math.Ceiling(uifont.xHeight);
            fm.DrawSpaces = (font.Italic == FontStyle.fontStyleItalic || font.Decoration != 0) ? 1 : 0;
            /*
            fm.Ascent = 10;
            fm.Descent = 4;
            fm.Height = 14;
            fm.XHeight = 14;
            fm.DrawSpaces = 1;*/
        }

        protected override void GetDefaultsCb(ref Defaults defaults)
        {             
            using var font = UIFont.SystemFontOfSize(12);
            defaults.FontFaceName = font.FamilyName; // default
            defaults.FontSize = 17; // iOS default in pt            
        }

        protected override MauiSize GetImageSize(UIImage image)
        {
            return new MauiSize((int)(image.Size.Width * image.CurrentScale), (int)(image.Size.Height * image.CurrentScale));
        }

        protected override int PtToPxCb(int pt)
        {
            return (int)(pt / UIScreen.MainScreen.Scale);
        }

        protected override void SetCursorCb(string cursor)
        {
            throw new NotImplementedException();
        }

        protected override int TextWidthCb(string text, ref FontDesc font)
        {
            var attributes = FontAttributesFromFontDesc(ref font);
            return (int)Math.Ceiling(new NSString(text).GetSizeUsingAttributes(attributes).Width);
        }

        private UIStringAttributes FontAttributesFromFontDesc(ref FontDesc font)
        {
            var uifont = UIFont.FromName(font.FaceName, font.Size);
            var attributes = new UIStringAttributes();
            attributes.Font = uifont;
            
            return attributes;
        }

        [MemberNotNullWhen(true, nameof(_hdc))]
        private bool VerifyContext()
        {
            return _hdc != null;
        }

        public override void DrawDocument(CGContext hdc, int width, int height)
        {
            _hdc = hdc;  
            base.DrawDocument(hdc, width, height);
            _hdc = null;
        }

        private static CGColor Color(WebColor webColor)
        {
            return new CGColor(webColor.Red / 255f, webColor.Green / 255f, webColor.Blue / 255f, webColor.Alpha / 255f);
        }

        private static CGPath CreateRoundedRect(ref Borders borders, ref Position draw_pos, bool forFill)
        {

            var radii = new Radii(ref borders, !forFill);

            var rect = new CGRect(draw_pos.X, draw_pos.Y, draw_pos.Width, draw_pos.Height);
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
            var path = new CGPath();

            path.MoveToPoint(point.X, point.Y);
            path.AddLineToPoint(point2.X, point2.Y);

            float radius = (float)(rect.Right - point2.X);
            float startAngle = (float)(point3.Y - rect.Y);
            float halfstroke = (float)(borders.Left.Width) / 2;

            if (Math.Abs(radius) > halfstroke || Math.Abs(startAngle) > halfstroke)
            {
                path.AddArc(point3.X, point3.Y, radius, startAngle, (float)Math.PI / 2, true);
            }
            path.AddLineToPoint(point4.X, point4.Y);
            radius = (float)(rect.Right - point5.X);
            startAngle = (float)(rect.Bottom - point4.Y);
            if (Math.Abs(radius) > halfstroke || Math.Abs(startAngle) > halfstroke)
            {
                path.AddArc(point5.X, point5.Y, radius, startAngle, (float)Math.PI / 2, true);
            }
            path.AddLineToPoint(point6.X, point6.Y);
            radius = (float)(point6.X - rect.X);
            startAngle = (float)(rect.Bottom - point7.Y);
            if (Math.Abs(radius) > halfstroke || Math.Abs(startAngle) > halfstroke)
            {
                path.AddArc(point7.X, point7.Y, radius, startAngle, (float)Math.PI / 2, true);
            }
            path.AddLineToPoint(point8.X, point8.Y);
            radius = (float)(point.X - rect.X);
            startAngle = (float)(point8.Y - rect.Y);
            if (Math.Abs(radius) > halfstroke || Math.Abs(startAngle) > halfstroke)
            {
                path.AddArc(point.X, point.Y, radius, startAngle, (float)Math.PI / 2, true);
            }
                                   

            path.CloseSubpath();
            return path;

            Vector2 clampPositive(Vector2 v)
            {
                if (v.X < 0) v.X = 0;
                if (v.Y < 0) v.Y = 0;
                return v;
            }
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
