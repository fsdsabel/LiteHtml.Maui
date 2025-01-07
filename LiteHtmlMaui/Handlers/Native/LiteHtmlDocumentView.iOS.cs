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
    class IOSLiteHtmlDocumentView : LiteHtmlDocumentView<CGContext, UIImage, UIFont>
    {
       

        private CGContext? _hdc;

        public IOSLiteHtmlDocumentView(LiteHtmlResolveResourceDelegate resolveResource, LiteHtmlRedrawView redrawView)
         : base(resolveResource, redrawView)
        {
        }

        protected override Task<UIImage> CreatePlatformBitmapAsync(Stream stream)
        {            
            using var imageData = NSData.FromStream(stream);
            return Task.FromResult(UIImage.LoadFromData(imageData!) ?? throw new InvalidOperationException("Cannot create platform bitmap from stream."));
        }

        protected override void DrawBackgroundCb(ref BackgroundPaint bg)
        {
            if (!VerifyContext()) return;

            if (bg.Color.Alpha > 0)
            {
                // we do not support multiple colors/thicknesses on borders or styles .. keep it simple, but this can be expanded if necessary

                var color = Color(bg.Color);

                var b = new Borders();
                b.Radius = bg.BorderRadius;
                var position = bg.OriginBox;

                using var path = CreateRoundedRect(ref b, ref position, true);
                _hdc.AddPath(path);
                _hdc.SetStrokeColor(color);
                _hdc.SetFillColor(color);
                _hdc.DrawPath(CGPathDrawingMode.FillStroke);
            }

            if (!string.IsNullOrEmpty(bg.Image))
            {
                // draw image
                var img = GetImage(CombineUrl(bg.BaseUrl, bg.Image));
                if (img != null)
                {
                    if (bg.repeat == background_repeat.background_repeat_no_repeat)
                    {
                        img.Image.Draw(new CGRect(bg.PositionX, bg.PositionY, bg.ImageSize.Width, bg.ImageSize.Height));
                    }
                    else
                    {
                        var rect = new CGRect(bg.PositionX, bg.PositionY, bg.ClipBox.Width, bg.ClipBox.Height);
                        _hdc.SaveState();
                        _hdc.SetPatternPhase(new CGSize(rect.Location));
                        switch (bg.repeat)
                        {
                            case background_repeat.background_repeat_repeat:
                                img.Image.DrawAsPatternInRect(rect);
                                break;
                            case background_repeat.background_repeat_repeat_x:
                                img.Image.DrawAsPatternInRect(new CGRect(rect.Location, new CGSize(rect.Width, bg.ImageSize.Height)));
                                break;
                            case background_repeat.background_repeat_repeat_y:
                                img.Image.DrawAsPatternInRect(new CGRect(rect.Location, new CGSize(bg.ImageSize.Width, rect.Height)));
                                break;
                        }
                        _hdc.RestoreState();
                    }
                    img.Release();
                }
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
            _hdc.AddPath(path);
            _hdc.SetStrokeColor(color);
            _hdc.SetLineWidth(borders.Right.Width);
            _hdc.SetLineDash(0, style);
            _hdc.StrokePath();            
        }

        protected override void DrawListMarkerCb(ref ListMarker listMarker, ref FontDesc font)
        {
            if (!VerifyContext()) return;
            if (string.IsNullOrEmpty(listMarker.Image))
            {
                var color = Color(listMarker.color);
                switch (listMarker.marker_type)
                {
                    case list_style_type.list_style_type_circle:
                        _hdc.BeginPath();
                        _hdc.AddEllipseInRect(new CGRect(listMarker.pos.X, listMarker.pos.Y, listMarker.pos.Width, listMarker.pos.Height));
                        _hdc.SetStrokeColor(color);
                        _hdc.StrokePath();
                        break;
                    case list_style_type.list_style_type_disc:
                        _hdc.BeginPath();
                        _hdc.AddEllipseInRect(new CGRect(listMarker.pos.X, listMarker.pos.Y, listMarker.pos.Width, listMarker.pos.Height));
                        _hdc.SetStrokeColor(color);
                        _hdc.SetFillColor(color);
                        _hdc.DrawPath(CGPathDrawingMode.FillStroke);
                        break;
                    case list_style_type.list_style_type_square:
                        _hdc.BeginPath();
                        _hdc.AddRect(new CGRect(listMarker.pos.X, listMarker.pos.Y, listMarker.pos.Width, listMarker.pos.Height));
                        _hdc.SetStrokeColor(color);
                        _hdc.SetFillColor(color);
                        _hdc.DrawPath(CGPathDrawingMode.FillStroke);
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
                        var attributes = FontAttributesFromFontDesc(ref font);
                        attributes.ForegroundColor = new UIColor(color);
                        new NSString(new string(text.Reverse().ToArray())).DrawString(new CGPoint(listMarker.pos.X, listMarker.pos.Y), attributes);
                        break;
                }
            }
        }

        protected override void DrawTextCb(IntPtr hdc, string text, ref FontDesc font, ref WebColor color, ref Position position)
        {
            if (!VerifyContext()) return;  
            var attributes = FontAttributesFromFontDesc(ref font);
            attributes.ForegroundColor = new UIColor(Color(color));

            // + 1 because we use Math.Floor in TextWidthCb, if we do not do this, characters will be cut off
            new NSString(text).DrawString(
                new CGRect(position.X, position.Y, position.Width + 1, position.Height), attributes);            
        }

        protected override void FillFontMetricsCb(ref FontDesc font, ref FontMetrics fm)
        {
            var uifont = CreateFont(ref font);

            fm.Ascent = (int)Math.Ceiling(uifont.Ascender - uifont.Descender);
            fm.Descent = -(int)uifont.Descender;
            fm.Height = (int)Math.Ceiling(uifont.LineHeight);
            fm.XHeight = (int)Math.Ceiling(uifont.XHeight);
            fm.CharWidth = TextWidthCb("0", ref font);
            fm.DrawSpaces = (font.Italic == FontStyle.fontStyleItalic || font.Decoration != 0) ? 1 : 0;
        }

        protected override void GetDefaultsCb(ref Defaults defaults)
        {             
            using var font = UIFont.SystemFontOfSize(12);
            defaults.FontFaceName = font.FamilyName; // default
            defaults.FontSize = PtToPxCb(17); // iOS default in pt            
        }

        protected override MauiSize GetImageSize(UIImage image)
        {
            return new MauiSize((int)(image.Size.Width * image.CurrentScale), (int)(image.Size.Height * image.CurrentScale));
        }

        protected override int PtToPxCb(int pt)
        {
            // iOS uses pt for drawing
            return pt;
        }

        private static int PxToPt(int px)
        {
            return px;
        }

        protected override void SetCursorCb(string cursor)
        {
        }

        protected override int TextWidthCb(string text, ref FontDesc font)
        {
            var attributes = FontAttributesFromFontDesc(ref font);
            return (int)Math.Floor(new NSString(text).GetSizeUsingAttributes(attributes).Width);
        }

        private UIStringAttributes FontAttributesFromFontDesc(ref FontDesc font)
        {
            var uifont = CreateFont(ref font);
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

        private static UIFont CreateFont(ref FontDesc font)
        {
            return ResolveFont(ref font, (font, force) =>
            {
                var uifont = UIFont.FromName(font.FaceName, PxToPt(font.Size));
                if (uifont == null && force)
                {
                    uifont = UIFont.SystemFontOfSize(PxToPt(font.Size));
                }
                return uifont;
            });            
        }

        private static CGPath CreateRoundedRect(ref Borders borders, ref Position draw_pos, bool forFill)
        {
            var path = new UIBezierPath();

            var minx = draw_pos.X;
            var miny = draw_pos.Y;
            var maxx = draw_pos.X + draw_pos.Width;
            var maxy = draw_pos.Y + draw_pos.Height;

            path.MoveTo(new CGPoint(minx + borders.Radius.top_left_x, miny));
            path.AddLineTo(new CGPoint(maxx - borders.Radius.top_right_x, miny));
            path.AddArc(new CGPoint(maxx - borders.Radius.top_right_x, miny + borders.Radius.top_right_x), radius: borders.Radius.top_right_x, startAngle: (NFloat) (3 * Math.PI / 2), endAngle: 0, true);
            path.AddLineTo(new CGPoint(maxx, maxy - borders.Radius.bottom_right_x));
            path.AddArc(new CGPoint(maxx - borders.Radius.bottom_right_x, maxy - borders.Radius.bottom_right_x), radius: borders.Radius.bottom_right_x, startAngle: 0, endAngle: (NFloat)(Math.PI / 2), true);
            path.AddLineTo(new CGPoint(minx + borders.Radius.bottom_left_x, maxy));
            path.AddArc(new CGPoint(minx + borders.Radius.bottom_left_x, maxy - borders.Radius.bottom_left_x), radius: borders.Radius.bottom_left_x, startAngle: (NFloat)(Math.PI / 2), endAngle: (NFloat)Math.PI, true);
            path.AddLineTo(new CGPoint(minx, miny + borders.Radius.top_left_x));
            path.AddArc(new CGPoint(minx + borders.Radius.top_left_x, miny + borders.Radius.top_left_x), radius: borders.Radius.top_left_x, startAngle: (NFloat)Math.PI, endAngle: (NFloat)(3 * Math.PI / 2), true);

            path.ClosePath();
            return path.CGPath!;
        }
    }
}
