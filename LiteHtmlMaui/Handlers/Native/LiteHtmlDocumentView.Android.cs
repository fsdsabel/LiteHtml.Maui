using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text;
using Android.Util;
using Android.Views;
using System.Diagnostics;
using Paint = Android.Graphics.Paint;
using Path = Android.Graphics.Path;
using Rect = Android.Graphics.Rect;
using RectF = Android.Graphics.RectF;

namespace LiteHtmlMaui.Handlers.Native
{
    

    class AndroidLiteHtmlDocumentView : LiteHtmlDocumentView<Canvas, Bitmap, Typeface>
    {
        private readonly Context _context;
        private readonly IFontManager _fontManager;
        private Canvas? _canvas;

        public AndroidLiteHtmlDocumentView(Context context, IFontManager fontManager, LiteHtmlResolveResourceDelegate resolveResource, LiteHtmlRedrawView redrawView)
            : base(resolveResource, redrawView)
        {
            _context = context;
            _fontManager = fontManager;
        }

        protected override int PtToPxCb(int pt)
        {
            using var metrics = _context.Resources?.DisplayMetrics;
            var result = TypedValue.ApplyDimension(ComplexUnitType.Sp, pt, metrics);
            return (int)result;            
        }

        protected override void GetDefaultsCb(ref Defaults defaults)
        {
            defaults.FontFaceName = ""; // default
            using var paint = new Paint();
            defaults.FontSize = PtToPxCb((int)Math.Ceiling(paint.TextSize));
        }

        protected override void DrawListMarkerCb(ref ListMarker listMarker, ref FontDesc font)
        {
            if (_canvas == null) return;

            if (string.IsNullOrEmpty(listMarker.Image))
            {
                var color = Android.Graphics.Color.Argb(listMarker.color.Alpha, listMarker.color.Red, listMarker.color.Green, listMarker.color.Blue);
                using var paint = new Paint(PaintFlags.AntiAlias);
                paint.Color = color;
                switch (listMarker.marker_type)
                {
                    case list_style_type.list_style_type_circle:
                        paint.SetStyle(Paint.Style.Stroke);
                        _canvas.DrawOval(listMarker.pos.X, listMarker.pos.Y, listMarker.pos.X + listMarker.pos.Width, listMarker.pos.Y + listMarker.pos.Height, paint);
                        break;
                    case list_style_type.list_style_type_disc:
                        paint.SetStyle(Paint.Style.FillAndStroke);
                        _canvas.DrawOval(listMarker.pos.X, listMarker.pos.Y, listMarker.pos.X + listMarker.pos.Width, listMarker.pos.Y + listMarker.pos.Height, paint);
                        break;
                    case list_style_type.list_style_type_square:
                        paint.SetStyle(Paint.Style.FillAndStroke);
                        _canvas.DrawRect(listMarker.pos.X, listMarker.pos.Y, listMarker.pos.X + listMarker.pos.Width, listMarker.pos.Y + listMarker.pos.Height, paint);
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
                        {
                            var pos = listMarker.pos;
                            var col = listMarker.color;
                            DrawTextCb(IntPtr.Zero, new string(text.Reverse().ToArray()), ref font, ref col, ref pos);                           
                        }
                        break;
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

            if (_canvas == null) return;

            // we do not support multiple colors/thicknesses on borders            
            using var paint = new Paint(PaintFlags.AntiAlias);
            var borderColor = borders.Right.Color;            
            paint.StrokeWidth = borders.Right.Width;
            paint.SetStyle(Paint.Style.Stroke);            
            paint.Color = Android.Graphics.Color.Argb(borderColor.Alpha, borderColor.Red, borderColor.Green, borderColor.Blue);
            var borderPath = CreateRoundedRect(ref borders, ref position);
            _canvas.DrawPath(borderPath, paint);
        }

        protected override void SetCursorCb(string cursor)
        {
        }

        protected override void DrawBackgroundCb(ref BackgroundPaint bg)
        {
            if (_canvas == null) return;

            if (bg.Color.Alpha > 0)
            {
                // we do not support multiple colors/thicknesses on borders or styles .. keep it simple, but this can be expanded if necessary
                using var paint = new Paint();
                var color = bg.Color;
                paint.SetStyle(Paint.Style.Fill);
                paint.Color = Android.Graphics.Color.Argb(color.Alpha, color.Red, color.Green, color.Blue);

                var b = new Borders();
                b.Radius = bg.BorderRadius;
                var position = bg.BorderBox;
                var path = CreateRoundedRect(ref b, ref position);

                _canvas.DrawPath(path, paint);
            }            

            if (!string.IsNullOrEmpty(bg.Image))
            {
                // draw image
                var img = GetImage(CombineUrl(bg.BaseUrl, bg.Image));
                if (img != null)
                {
                    using var paint = new Paint(PaintFlags.FilterBitmap);
                    if (bg.repeat == background_repeat.background_repeat_no_repeat)
                    {
                        _canvas.DrawBitmap(img.Image, null, new Rect(bg.PositionX, bg.PositionY, bg.PositionX + bg.ImageSize.Width, bg.PositionY + bg.ImageSize.Height), paint);
                    }
                    else
                    {
                        var rect = new Rect(bg.PositionX, bg.PositionY, bg.PositionX + bg.ClipBox.Width, bg.PositionY + bg.ClipBox.Height);
                        using var drawable = new BitmapDrawable(_context.Resources, img.Image);
                        drawable.SetTileModeXY(Shader.TileMode.Repeat, Shader.TileMode.Repeat);
                        
                        switch (bg.repeat)
                        {
                            case background_repeat.background_repeat_repeat:
                                drawable.SetBounds(rect.Left, rect.Top, rect.Right, rect.Bottom);
                                break;
                            case background_repeat.background_repeat_repeat_x:
                                drawable.SetBounds(rect.Left, rect.Top, rect.Right, rect.Top + bg.ImageSize.Height);
                                break;
                            case background_repeat.background_repeat_repeat_y:
                                drawable.SetBounds(rect.Left, rect.Top, rect.Left + bg.ImageSize.Width, rect.Bottom);
                                break;
                        }
                        drawable.Draw(_canvas);
                    }

                    
                    img.Release();
                }
            }            
        }

        protected override void FillFontMetricsCb(ref FontDesc font, ref FontMetrics fm)
        {
            using var paint = PaintFromFontDesc(font);
            using var metrics = paint.GetFontMetricsInt();
            if (metrics == null)
            {
                return;
            }
            fm.Ascent = -metrics.Ascent;
            fm.Descent = metrics.Descent;
            fm.Height = (int)Math.Ceiling(paint.FontSpacing);// metrics.Descent - metrics.Ascent;
            var bounds = new Rect();
            paint.GetTextBounds("x", 0, 1, bounds);
            fm.XHeight = bounds.Height();
            fm.DrawSpaces = (font.Italic == FontStyle.fontStyleItalic || font.Decoration != 0) ? 1 : 0;
        }

        protected override void DrawTextCb(IntPtr hdc, string text, ref FontDesc font, ref WebColor color, ref Position position)
        {
            if (_canvas == null) return;
            using var paint = PaintFromFontDesc(font);
            paint.Color = Android.Graphics.Color.Argb(color.Alpha, color.Red, color.Green, color.Blue);            
            using var layout = StaticLayout.Builder.Obtain(text, 0, text.Length, paint, position.Width).Build();
            _canvas.Save();
            _canvas.Translate(position.X, position.Y);
            layout.Draw(_canvas); 
            _canvas.Restore();
        }


        protected override int TextWidthCb(string text, ref FontDesc font)
        {
            using var paint = PaintFromFontDesc(font);
            return (int)Math.Ceiling(paint.MeasureText(text));
        }

        private TextPaint PaintFromFontDesc(FontDesc fontDesc)
        {
            var paint = new TextPaint(PaintFlags.SubpixelText | /*| PaintFlags.LinearText |*/ PaintFlags.AntiAlias);

            var typeface = ResolveFont(ref fontDesc, (fontDesc, force) =>
            {
                //TODO: get available fonts
                Typeface? typeface;
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.P)
                {
#pragma warning disable CA1416 // Validate platform compatibility
                    
                    typeface = Typeface.Create(Typeface.Create(fontDesc.FaceName, TypefaceStyle.Normal), fontDesc.Weight, fontDesc.Italic == FontStyle.fontStyleItalic);
#pragma warning restore CA1416 // Validate platform compatibility
                }
                else
                {
                    var style = TypefaceStyle.Normal;
                    if (fontDesc.Italic == FontStyle.fontStyleItalic)
                    {
                        style |= fontDesc.Weight >= 700 ? TypefaceStyle.BoldItalic : TypefaceStyle.Italic;
                    }
                    if (fontDesc.Weight >= 700)
                    {
                        style |= TypefaceStyle.Bold;
                    }
                    typeface = Typeface.Create(Typeface.Create(fontDesc.FaceName, TypefaceStyle.Normal), style);
                }
                return typeface;
            });

            paint.SetTypeface(typeface);
       
            paint.TextSize = fontDesc.Size;

            if (fontDesc.Decoration.HasFlag(font_decoration.Underline))
            {
                paint.Flags = paint.Flags | PaintFlags.UnderlineText;
            }
            if (fontDesc.Decoration.HasFlag(font_decoration.Linethrough))
            {
                paint.Flags = paint.Flags | PaintFlags.StrikeThruText;
            }
            
            return paint;
        }

        private static Path CreateRoundedRect(ref Borders borders, ref Position draw_pos)
        {
            var path = new Path();

            path.AddRoundRect(new RectF(draw_pos.X, draw_pos.Y, draw_pos.X + draw_pos.Width, draw_pos.Y + draw_pos.Height),
                new float[] { 
                    borders.Radius.top_left_x,
                    borders.Radius.top_left_y,
                    borders.Radius.top_right_x,
                    borders.Radius.top_right_y,
                    borders.Radius.bottom_right_x,
                    borders.Radius.bottom_right_y,
                    borders.Radius.bottom_left_x,
                    borders.Radius.bottom_left_y
                }, Path.Direction.Cw!);

            return path;
        }

        public override void DrawDocument(Canvas canvas, int width, int height)
        {
            _canvas = canvas;
            base.DrawDocument(canvas, width, height);
            _canvas = null;
        }

        protected override Task<Bitmap> CreatePlatformBitmapAsync(System.IO.Stream stream)
        {
            return Task.FromResult(BitmapFactory.DecodeStreamAsync(stream).GetAwaiter().GetResult() ?? throw new InvalidOperationException("Cannot create platform bitmap from stream."));
        }

        protected override MauiSize GetImageSize(Bitmap image)
        {
            return new MauiSize(image.Width, image.Height);
        }
    }
}
