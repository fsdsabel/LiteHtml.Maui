using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using System.Diagnostics;
using Paint = Android.Graphics.Paint;
using Path = Android.Graphics.Path;
using Rect = Android.Graphics.Rect;
using RectF = Android.Graphics.RectF;

namespace LiteHtmlMaui.Handlers.Native
{
    

    class AndroidLiteHtmlDocumentView : LiteHtmlDocumentView<Canvas, Bitmap>
    {
        private readonly Context _context;
        private Canvas? _canvas;

        public AndroidLiteHtmlDocumentView(Context context, LiteHtmlResolveResourceDelegate resolveResource)
            : base(resolveResource)
        {
            _context = context;
        }

        protected override int PtToPxCb(int pt)
        {
            using var metrics = _context.Resources?.DisplayMetrics;            
            return (int)TypedValue.ApplyDimension(ComplexUnitType.Pt, pt, metrics);
        }

        protected override void GetDefaultsCb(ref Defaults defaults)
        {
            defaults.FontFaceName = ""; // default
            using var paint = new Paint();
            defaults.FontSize = PtToPxCb((int)Math.Ceiling(paint.TextSize));
        }

        protected override void DrawListMarkerCb(ref ListMarker listMarker, ref FontDesc font)
        {
            //throw new NotImplementedException();
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

            

            if (!string.IsNullOrEmpty(bg.Image))
            {
                // draw image
                var img = GetImage(CombineUrl(bg.BaseUrl, bg.Image));
                if (img != null)
                {
                    using var paint = new Paint(PaintFlags.FilterBitmap);
                    _canvas.DrawBitmap(img.Image, null, new Rect(bg.PositionX, bg.PositionY, (int)bg.ImageSize.Width, (int)bg.ImageSize.Height), paint);
                    img.Release();
                }
            }
            else
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
            fm.Height = fm.XHeight = metrics.Descent - metrics.Ascent; 
            fm.DrawSpaces = (font.Italic == FontStyle.fontStyleItalic || font.Decoration != 0) ? 1 : 0;
        }

        protected override void DrawTextCb(IntPtr hdc, string text, ref FontDesc font, ref WebColor color, ref Position position)
        {
            if (_canvas == null) return;
            using var paint = PaintFromFontDesc(font);
            paint.Color = Android.Graphics.Color.Argb(color.Alpha, color.Red, color.Green, color.Blue);
            
            _canvas.DrawText(text, position.X, position.Height + position.Y, paint);
        }


        protected override int TextWidthCb(string text, ref FontDesc font)
        {
            using var paint = PaintFromFontDesc(font);
            return (int)Math.Ceiling(paint.MeasureText(text));
        }

        private Paint PaintFromFontDesc(FontDesc fontDesc)
        {
            var paint = new Paint(PaintFlags.SubpixelText | /*| PaintFlags.LinearText |*/ PaintFlags.AntiAlias);
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
                if(fontDesc.Italic == FontStyle.fontStyleItalic)
                {
                    style |= fontDesc.Weight >= 700 ? TypefaceStyle.BoldItalic : TypefaceStyle.Italic;
                }
                if(fontDesc.Weight>=700)
                {
                    style |= TypefaceStyle.Bold;
                }
                typeface = Typeface.Create(Typeface.Create(fontDesc.FaceName, TypefaceStyle.Normal), style);
            }
            paint.SetTypeface(typeface);
                        
            var spSize = PxToPt(fontDesc.Size, _context);
            paint.TextSize = TypedValue.ApplyDimension(ComplexUnitType.Sp, spSize, _context.Resources?.DisplayMetrics);
            Debug.WriteLine($"{spSize} -> {paint.TextSize}");

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

        private static int PxToPt(float px, Context context)
        {
            using var metrics = context.Resources?.DisplayMetrics;
            return (int)Math.Ceiling(px * 72.0 / (double)(metrics?.DensityDpi ?? DisplayMetricsDensity.Default));
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

            /*
            var radii = new Radii(ref borders, false);

            var rect = new Rectangle(draw_pos.X, draw_pos.Y, draw_pos.Width, draw_pos.Height);
            var point = new Vector2(radii.LeftTop, 0.0f);
            var point2 = new Vector2(rect.Width - radii.RightTop, 0.0f);
            var point3 = new Vector2(rect.Width, radii.TopRight);
            var point4 = new Vector2(rect.Width, rect.Height - radii.BottomRight);
            var point5 = new Vector2(rect.Width - radii.RightBottom, rect.Height);
            var point6 = new Vector2(radii.LeftBottom, rect.Height);
            var point7 = new Vector2(0.0f, rect.Height - radii.BottomLeft);
            var point8 = new Vector2(0.0f, radii.TopLeft);
            if (point.X > point2.X)
            {
                float x = radii.LeftTop / (radii.LeftTop + radii.RightTop) * rect.Width;
                point.X = x;
                point2.X = x;
            }
            if (point3.Y > point4.Y)
            {
                float y = radii.TopRight / (radii.TopRight + radii.BottomRight) * rect.Height;
                point3.Y = y;
                point4.Y = y;
            }
            if (point5.X < point6.X)
            {
                float x2 = radii.LeftBottom / (radii.LeftBottom + radii.RightBottom) * rect.Width;
                point5.X = x2;
                point6.X = x2;
            }
            if (point7.Y < point8.Y)
            {
                float y2 = radii.TopLeft / (radii.TopLeft + radii.BottomLeft) * rect.Height;
                point7.Y = y2;
                point8.Y = y2;
            }
            var vector = new Vector2(new float[] { rect.X, rect.Y });
            
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
            var path = new Path();

            path.MoveTo(point.X, point.Y);
            path.LineTo(point2.X, point2.Y);

            float num = rect.Right - point2.X;
            float num2 = point3.Y - rect.Y;
            float halfstroke = (float)(borders.Left.Width) / 2;

            if (Math.Abs(num) > halfstroke || Math.Abs(num2) > halfstroke)
            {
                path.ArcTo(point3.X, point3.Y, num, num2, 0.0f, 90, true);
                //ctx->ArcTo(point3, Size(num, num2), 0.0, false, SweepDirection::Clockwise, true, false);
            }
            path.LineTo(point4.X, point4.Y); 
            num = rect.Right - point5.X;
            num2 = rect.Bottom - point4.Y;
            if (Math.Abs(num) > halfstroke || Math.Abs(num2) > halfstroke)
            {
                path.ArcTo(point5.X, point5.Y, num, num2, 0.0f, 90, true);
                //ctx->ArcTo(point5, Size(num, num2), 0.0, false, SweepDirection::Clockwise, true, false);
            }
            path.LineTo(point6.X, point6.Y);
            num = point6.X - rect.X;
            num2 = rect.Bottom - point7.Y;
            if (Math.Abs(num) > halfstroke || Math.Abs(num2) > halfstroke)
            {
                path.ArcTo(point7.X, point7.Y, num, num2, 0.0f, 90, true);
                //ctx->ArcTo(point7, Size(num, num2), 0.0, false, SweepDirection::Clockwise, true, false);
            }
            path.LineTo(point8.X, point8.Y);
            num = point.X - rect.X;
            num2 = point8.Y - rect.Y;
            if (Math.Abs(num) > halfstroke || Math.Abs(num2) > halfstroke)
            {
                path.ArcTo(point.X, point.Y, num, num2, 0.0f, 90, true);
                //ctx->ArcTo(point, Size(num, num2), 0.0, false, SweepDirection::Clockwise, true, false);
            }

            return path;

            Vector2 clampPositive(Vector2 v)
            {
                if (v.X < 0) v.X = 0;
                if (v.Y < 0) v.Y = 0;
                return v;
            }*/
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
