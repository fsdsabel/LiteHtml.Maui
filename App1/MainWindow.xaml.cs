using LiteHtmlMaui.Handlers.Native;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App1
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
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
        private static readonly Dictionary<(string faceName, int weight, LiteHtmlMaui.Handlers.Native.FontStyle fontStyle), CanvasFontMetrics> _fontMetricsCache = new Dictionary<(string faceName, int weight, LiteHtmlMaui.Handlers.Native.FontStyle fontStyle), CanvasFontMetrics>();
        private CanvasDrawingSession _drawingSession;

        public MainWindow()
        {
            this.InitializeComponent();
            
        }

        private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            _drawingSession = args.DrawingSession;
            var fd = new FontDesc
            {
                Size = 8,
                Decoration = font_decoration.Underline | font_decoration.Linethrough,
                FaceName = "Arial",
                Weight = 100
            };
            var color = new WebColor { Alpha = 255 };
            var pos = new Position { X = 0, Y = 0, Width = 100, Height = 100 };
            DrawTextCb(IntPtr.Zero, "Some text", ref fd, ref color, ref pos);
            _drawingSession = null;
        }

        private CanvasFontMetrics GetCanvasFontMetrics(ref FontDesc font)
        {
            if (_fontMetricsCache.TryGetValue((font.FaceName, font.Weight, font.Italic), out var metrics))
            {
                return metrics;
            }

            using var fonts = CanvasFontSet.GetSystemFontSet().GetMatchingFonts(
              font.FaceName,
              new FontWeight((ushort)font.Weight),
              FontStretch.Normal,
              font.Italic == LiteHtmlMaui.Handlers.Native.FontStyle.fontStyleItalic ? Windows.UI.Text.FontStyle.Italic : Windows.UI.Text.FontStyle.Normal);
            var winfont = fonts.Fonts.FirstOrDefault();
            if (winfont == null)
            {
                winfont = CanvasFontSet.GetSystemFontSet().Fonts.First();
            }

            metrics = new CanvasFontMetrics(winfont);
            _fontMetricsCache.Add((font.FaceName, font.Weight, font.Italic), metrics);

            winfont.Dispose();
            return metrics;
        }
        private static CanvasTextFormat CreateTextFormatFromFontDesc(ref FontDesc fontDesc)
        {
            var format = new CanvasTextFormat();

            format.FontFamily = fontDesc.FaceName;
            format.FontSize = fontDesc.Size;
            format.FontWeight = new FontWeight((ushort)fontDesc.Weight);
            format.FontStyle = fontDesc.Italic == LiteHtmlMaui.Handlers.Native.FontStyle.fontStyleItalic ? Windows.UI.Text.FontStyle.Italic : Windows.UI.Text.FontStyle.Normal;
            format.Options = CanvasDrawTextOptions.EnableColorFont;
            format.WordWrapping = CanvasWordWrapping.NoWrap;

            return format;
        }

        private void DrawTextCb(IntPtr hdc, string text, ref FontDesc font, ref WebColor color, ref Position position)
        {
           // if (_drawingSession == null) return;

            using var format = CreateTextFormatFromFontDesc(ref font);
            using var brush = new CanvasSolidColorBrush(_drawingSession.Device, Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue));
            using var textLayout = new CanvasTextLayout(_drawingSession.Device, text, format, position.Width, position.Height);

            CanvasFontMetrics? winfont = null;
            if (font.Decoration.HasFlag(font_decoration.Underline))
            {
                winfont = winfont ?? GetCanvasFontMetrics(ref font);
                //var underlinePosition = (float)Math.Ceiling(position.Y + font.Size * winfont.Ascent - font.Size * winfont.UnderlinePosition) + 0.5f;
                var underlinePosition = (winfont.Ascent + winfont.Descent) * font.Size - 0.5f;
                _drawingSession.DrawLine(position.X, underlinePosition, position.X + position.Width, underlinePosition, brush, font.Size * winfont.UnderlineThickness);
                // TODO .. correct position?
                textLayout.SetUnderline(0, text.Length, true);

            }
            if (font.Decoration.HasFlag(font_decoration.Linethrough))
            {
                winfont = winfont ?? GetCanvasFontMetrics(ref font);
                //var strikethroughPosition = (float)Math.Floor(position.Y + font.Size * winfont.Ascent - font.Size * winfont.StrikethroughPosition) + 0.5f;
                //_drawingSession.DrawLine(position.X, strikethroughPosition, position.X + position.Width, strikethroughPosition, brush, font.Size * winfont.StrikethroughThickness);
                var strikethroughPosition = position.Y + (winfont.Ascent + winfont.Descent) * font.Size * 0.6f - 0.5f;
                _drawingSession.DrawLine(position.X, strikethroughPosition, position.X + position.Width, strikethroughPosition, brush, font.Size * winfont.StrikethroughThickness);
               // _drawingSession.DrawLine((float)textRect.Left, (float)y, (float)textRect.Right, (float)y, color, 1.2f);
                // TODO .. correct position?
                textLayout.SetStrikethrough(0, text.Length, true);
            }
            if (font.Decoration.HasFlag(font_decoration.Overline))
            {
                winfont = winfont ?? GetCanvasFontMetrics(ref font);
                var overlinePosition = position.Y + 0.5f;
                _drawingSession.DrawLine(position.X, overlinePosition, position.X + position.Width, overlinePosition, brush, font.Size * winfont.UnderlineThickness);
            }

            _drawingSession.DrawTextLayout(textLayout, position.X, position.Y, brush);

        }
    }
}
